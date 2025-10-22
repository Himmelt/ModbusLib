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

    public int Timeout { get; set; } = 5000; // 默认5秒超时（5000毫秒）

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
        if (Timeout >= 0) {
            cts.CancelAfter(Timeout);
        }

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
        // 创建一个足够大的缓冲区来接收数据
        // 对于大多数Modbus响应来说，512字节已经足够
        var buffer = ArrayPool<byte>.Shared.Rent(512);

        try {
            // 读取响应数据
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (Timeout >= 0) {
                cts.CancelAfter(Timeout);
            }

            var totalBytesRead = 0;
            var bytesRead = 0;

            // 先读取前几个字节来确定响应长度
            while (totalBytesRead < 6 && !cts.Token.IsCancellationRequested) {
                bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, 6 - totalBytesRead), cts.Token).ConfigureAwait(false);
                if (bytesRead == 0) {
                    throw new ModbusCommunicationException("连接意外关闭");
                }
                totalBytesRead += bytesRead;
            }

            // 根据协议类型确定完整的响应长度
            // 这里我们先尝试读取更多数据，直到没有更多数据或超时
            try {
                while (totalBytesRead < buffer.Length && !cancellationToken.IsCancellationRequested) {
                    // 设置一个较短的超时时间来检测数据结束
                    using var quickCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    quickCts.CancelAfter(100); // 100ms超时

                    bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, buffer.Length - totalBytesRead), quickCts.Token).ConfigureAwait(false);
                    if (bytesRead == 0) {
                        break; // 没有更多数据
                    }
                    totalBytesRead += bytesRead;
                }
            } catch (OperationCanceledException) {
                // 正常情况，表示没有更多数据
            }

            // 返回实际读取的数据
            var result = new byte[totalBytesRead];
            Array.Copy(buffer, 0, result, 0, totalBytesRead);
            return result;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw new ModbusTimeoutException($"读取响应超时");
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
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