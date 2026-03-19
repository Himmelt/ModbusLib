using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using System.Buffers;
using System.IO.Pipelines;

namespace ModbusLib.Transports;

public class PipeTransport : IModbusTransport {

    private readonly Pipe _in;
    private readonly Pipe _out;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public int Timeout { get; set; } = 5000;

    public bool IsConnected => !_disposed && _in != null && _out != null;

    public PipeTransport(Pipe pipeIn, Pipe pipeOut, int timeout = 5000) {
        _in = pipeIn ?? throw new ArgumentNullException(nameof(pipeIn));
        _out = pipeOut ?? throw new ArgumentNullException(nameof(pipeOut));
        Timeout = timeout;
    }

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) {
            throw new ModbusConnectionException("Stream不可用或未连接");
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
            await _out.Writer.WriteAsync(request, cts.Token).ConfigureAwait(false);
            await _out.Writer.FlushAsync(cts.Token).ConfigureAwait(false);

            var response = await ReceiveResponseAsync(_in, cts.Token).ConfigureAwait(false);
            return response;
        } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) {
            throw new ModbusCommunicationException($"Pipe 通信异常: {ex.Message}", ex);
        } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
            throw new ModbusTimeoutException("Pipe 通信超时，操作已取消");
        } finally {
            if (_semaphore.CurrentCount == 0) {
                _semaphore.Release();
            }
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(Pipe inPipe, CancellationToken cancellationToken) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Timeout >= 0) {
            timeoutCts.CancelAfter(Timeout);
        }

        using var memoryStream = new MemoryStream();

        while (!timeoutCts.Token.IsCancellationRequested) {
            var result = await inPipe.Reader.ReadAsync(timeoutCts.Token).ConfigureAwait(false);
            var data = result.Buffer;

            if (data.IsEmpty && result.IsCompleted) {
                break;
            }

            if (!data.IsEmpty) {
                foreach (var segment in data) {
                    memoryStream.Write(segment.Span);
                }
                inPipe.Reader.AdvanceTo(data.End);
            }

            if (result.IsCompleted) {
                break;
            }
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