using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using System.Threading.Channels;

namespace ModbusLib.Transports;

public class ChannelTransport(ChannelSession session, int timeout = 5000) : IModbusTransport {

    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ChannelSession _session = session ?? throw new ArgumentNullException(nameof(session));

    public int Timeout { get; set; } = timeout;
    public bool IsConnected => !_disposed && _session != null;

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) {
            throw new ModbusConnectionException("Channel 不可用或未连接");
        }

        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if (_disposed) return Task.CompletedTask;
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) throw new ModbusConnectionException("Channel 不可用");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Timeout >= 0) {
            cts.CancelAfter(Timeout);
        }

        await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
        try {
            await _session.ClientToServer.Writer.WriteAsync(request, cts.Token).ConfigureAwait(false);

            var response = await ReceiveResponseAsync(cts.Token).ConfigureAwait(false);
            return response;
        } catch (Exception ex) when (ex is ChannelClosedException || ex is ObjectDisposedException) {
            throw new ModbusCommunicationException($"Channel 通信异常: {ex.Message}", ex);
        } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
            throw new ModbusTimeoutException("Channel 通信超时，操作已取消");
        } finally {
            _semaphore.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancellationToken) {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Timeout >= 0) {
            timeoutCts.CancelAfter(Timeout);
        }

        try {
            var response = await _session.ServerToClient.Reader.ReadAsync(timeoutCts.Token).ConfigureAwait(false);
            
            if (response.Length == 0) {
                throw new ModbusTimeoutException("Channel接收超时，未收到响应数据");
            }

            return response;
        } catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
            throw new ModbusTimeoutException("Channel接收超时，未收到响应数据");
        }
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
