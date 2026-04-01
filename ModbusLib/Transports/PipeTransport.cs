using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Transports;

public sealed class PipeTransport(PipeSession session) : IModbusTransport {

    private bool _disposed;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public int Timeout { get; set; } = -1;
    public bool IsConnected => !_disposed;

    public Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken cancelToken = default) {
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        cts.CancelAfter(Timeout);

        await _lock.WaitAsync(cts.Token).ConfigureAwait(false);
        try {
            await session.ClientToServer.Writer.WriteAsync(request, cts.Token).ConfigureAwait(false);
            await session.ClientToServer.Writer.FlushAsync(cts.Token).ConfigureAwait(false);
            var response = await ReceiveResponseAsync(cts.Token).ConfigureAwait(false);
            return response;
        } catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancelToken.IsCancellationRequested) {
            throw new ModbusTimeoutException("Pipe 通信超时，操作已取消");
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"Pipe 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        using var memoryStream = new MemoryStream();
        var result = await session.ServerToClient.Reader.ReadAsync(cancelToken).ConfigureAwait(false);
        var data = result.Buffer;
        if (!data.IsEmpty) {
            foreach (var segment in data) {
                memoryStream.Write(segment.Span);
            }
            session.ServerToClient.Reader.AdvanceTo(data.End);
        }
        var response = memoryStream.ToArray();
        if (response.Length == 0) throw new ModbusTimeoutException("Pipe 接收超时，未收到响应数据");
        return response;
    }

    public void Dispose() {
        if (_disposed) return;
        _lock?.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }
}