using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using System.IO.Pipelines;

namespace ModbusLib.Transports;

public class PipeTransport(Pipe serverToClient, Pipe clientToServer, int timeout = 5000) : IModbusTransport {

    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Pipe serverToClient = serverToClient ?? throw new ArgumentNullException(nameof(serverToClient));
    private readonly Pipe clientToServer = clientToServer ?? throw new ArgumentNullException(nameof(clientToServer));

    public int Timeout { get; set; } = timeout;
    public bool IsConnected => !_disposed && serverToClient != null && clientToServer != null;

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) {
            throw new ModbusConnectionException("Pipe 不可用或未连接");
        }

        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if (_disposed) return Task.CompletedTask;
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) throw new ModbusConnectionException("Pipe 不可用");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Timeout >= 0) {
            cts.CancelAfter(Timeout);
        }

        await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
        try {
            await clientToServer.Writer.WriteAsync(request, cts.Token).ConfigureAwait(false);
            await clientToServer.Writer.FlushAsync(cts.Token).ConfigureAwait(false);

            var response = await ReceiveResponseAsync(serverToClient, cts.Token).ConfigureAwait(false);
            return response;
        } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) {
            throw new ModbusCommunicationException($"Pipe 通信异常: {ex.Message}", ex);
        } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
            throw new ModbusTimeoutException("Pipe 通信超时，操作已取消");
        } finally {
            _semaphore.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(Pipe inPipe, CancellationToken cancellationToken) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Timeout >= 0) {
            timeoutCts.CancelAfter(Timeout);
        }

        using var memoryStream = new MemoryStream();

        try {
            var result = await inPipe.Reader.ReadAsync(timeoutCts.Token).ConfigureAwait(false);
            var data = result.Buffer;

            if (!data.IsEmpty) {
                foreach (var segment in data) {
                    memoryStream.Write(segment.Span);
                }
                inPipe.Reader.AdvanceTo(data.End);
            }
        } catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
        }

        var response = memoryStream.ToArray();
        if (response.Length == 0) {
            throw new ModbusTimeoutException("Pipe接收超时，未收到响应数据");
        }

        return response;
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        _disposed = true;

        if (disposing) {
            _semaphore?.Dispose();
        }
    }

    public async ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore() {
        if (_disposed)
            return;

        _disposed = true;

        try {
            await DisconnectAsync().ConfigureAwait(false);
        } catch {
        }

        _semaphore?.Dispose();
    }
}