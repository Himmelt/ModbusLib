using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Transports;

/// <summary>
/// TCP传输实现
/// </summary>
public class TcpTransport(NetworkConnectionConfig config) : IModbusTransport {
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly NetworkConnectionConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool IsConnected => _tcpClient?.Connected == true && _stream != null;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (IsConnected)
                return true;

            await DisconnectInternalAsync().ConfigureAwait(false);

            // 如果指定了本地端口，则绑定到该端口
            if (_config.LocalPort.HasValue) {
                _tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, _config.LocalPort.Value));
            } else {
                _tcpClient = new TcpClient();
            }

            // 配置TCP选项
            _tcpClient.ReceiveTimeout = _config.ReceiveTimeout;
            _tcpClient.SendTimeout = _config.SendTimeout;
            _tcpClient.ReceiveBufferSize = _config.ReceiveBufferSize;
            _tcpClient.SendBufferSize = _config.SendBufferSize;

            // 连接到服务器
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(_config.ConnectTimeout);

            await _tcpClient.ConnectAsync(_config.Host, _config.RemotePort, connectCts.Token).ConfigureAwait(false);

            // 配置Socket选项
            if (_tcpClient.Client != null) {
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, _config.KeepAlive);
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, _config.NoDelay);
            }

            _stream = _tcpClient.GetStream();
            _stream.ReadTimeout = _config.ReceiveTimeout;
            _stream.WriteTimeout = _config.SendTimeout;

            return true;
        } catch (Exception ex) {
            await DisconnectInternalAsync().ConfigureAwait(false);
            throw new ModbusConnectionException($"TCP连接失败: {ex.Message}", ex);
        } finally {
            _semaphore.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if (_disposed)
            return;

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            await DisconnectInternalAsync().ConfigureAwait(false);
        } finally {
            _semaphore.Release();
        }
    }

    private async Task DisconnectInternalAsync() {
        try {
            if (_stream != null) {
                await _stream.FlushAsync().ConfigureAwait(false);
                _stream.Close();
                _stream = null;
            }

            if (_tcpClient != null) {
                _tcpClient.Close();
                _tcpClient = null;
            }
        } catch {
            // 忽略断开连接时的异常
        }
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected)
            throw new ModbusConnectionException("TCP连接未建立");

        // 使用超时取消令牌
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        await _semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
        try {
            var stream = _stream!;

            // 发送请求
            await stream.WriteAsync(request, cts.Token).ConfigureAwait(false);
            await stream.FlushAsync(cts.Token).ConfigureAwait(false);

            // 接收响应
            var response = await ReceiveResponseAsync(stream, cts.Token).ConfigureAwait(false);
            return response;
        } catch (Exception ex) when (ex is SocketException || ex is IOException) {
            await DisconnectInternalAsync().ConfigureAwait(false);
            throw new ModbusCommunicationException($"TCP [{_config.Host}:{_config.RemotePort}] 通信异常: {ex.Message}", ex);
        } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
            throw new ModbusTimeoutException($"TCP [{_config.Host}:{_config.RemotePort}] 通信超时，取消");
        } catch (TimeoutException) {
            throw new ModbusTimeoutException($"TCP [{_config.Host}:{_config.RemotePort}] 通信超时");
        } finally {
            if (_semaphore.CurrentCount == 0) {  // 防止重复释放
                _semaphore.Release();
            }
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(NetworkStream stream, CancellationToken cancellationToken) {
        const int headerSize = 6; // MBAP header size
        var headerBuffer = ArrayPool<byte>.Shared.Rent(headerSize);

        try {
            // 读取MBAP头部
            await ReadExactAsync(stream, headerBuffer, headerSize, cancellationToken).ConfigureAwait(false);

            // 解析长度字段（字节4-5）
            var length = (ushort)((headerBuffer[4] << 8) | headerBuffer[5]);

            // 读取剩余数据
            var remainingSize = length;// - 1; // 减去单元ID字节
            var fullBuffer = ArrayPool<byte>.Shared.Rent(headerSize + remainingSize);

            try {
                // 复制头部数据
                Array.Copy(headerBuffer, 0, fullBuffer, 0, headerSize);

                // 读取剩余数据
                if (remainingSize > 0) {
                    await ReadExactAsync(stream, fullBuffer, headerSize, remainingSize, cancellationToken).ConfigureAwait(false);
                }

                var result = new byte[headerSize + remainingSize];
                Array.Copy(fullBuffer, 0, result, 0, result.Length);

                return result;
            } finally {
                ArrayPool<byte>.Shared.Return(fullBuffer);
            }
        } finally {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    private async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken cancellationToken) {
        var totalBytesRead = 0;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        while (totalBytesRead < count) {
            // 检查取消令牌
            cts.Token.ThrowIfCancellationRequested();

            try {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, count - totalBytesRead), cts.Token).ConfigureAwait(false);
                if (bytesRead == 0) {
                    throw new ModbusCommunicationException("连接意外关闭");
                }
                totalBytesRead += bytesRead;
            } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
                throw new ModbusTimeoutException($"读取超时，期望{count}字节，实际读取{totalBytesRead}字节");
            }
        }
    }

    private async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        var totalBytesRead = 0;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        while (totalBytesRead < count) {
            // 检查取消令牌
            cts.Token.ThrowIfCancellationRequested();

            try {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), cts.Token).ConfigureAwait(false);
                if (bytesRead == 0) {
                    throw new ModbusCommunicationException("连接意外关闭");
                }
                totalBytesRead += bytesRead;
            } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
                throw new ModbusTimeoutException($"读取超时，期望{count}字节，实际读取{totalBytesRead}字节");
            }
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
            try {
                DisconnectInternalAsync().Wait(1000);
            } catch {
                // 忽略释放时的异常
            }

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
            // 忽略释放时的异常
        }

        _semaphore?.Dispose();
    }
}