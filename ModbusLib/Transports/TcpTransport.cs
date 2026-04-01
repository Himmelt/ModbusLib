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
public sealed class TcpTransport(NetworkConfig config) : IModbusTransport {

    private bool _disposed;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public int Timeout { get; set; } = -1;
    public bool IsConnected => _tcpClient?.Connected == true && _stream != null;

    public async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        try {
            if (IsConnected) return true;

            await DisconnectInternalAsync().ConfigureAwait(false);

            var localIP = string.IsNullOrWhiteSpace(config.LocalHost) ? IPAddress.Any : IPAddress.Parse(config.LocalHost);
            var localPort = config.LocalPort ?? 0;
            _tcpClient = new TcpClient(new IPEndPoint(localIP, localPort)) {
                ReceiveTimeout = config.ReceiveTimeout,
                SendTimeout = config.SendTimeout,
                ReceiveBufferSize = config.ReceiveBufferSize,
                SendBufferSize = config.SendBufferSize
            };

            // 连接到服务器
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            cts.CancelAfter(config.ConnectTimeout);

            await _tcpClient.ConnectAsync(config.RemoteHost, config.RemotePort, cts.Token).ConfigureAwait(false);

            // 配置Socket选项
            if (_tcpClient.Client != null) {
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }

            _stream = _tcpClient.GetStream();
            _stream.ReadTimeout = config.ReceiveTimeout;
            _stream.WriteTimeout = config.SendTimeout;

            return true;
        } catch (Exception ex) {
            await DisconnectInternalAsync().ConfigureAwait(false);
            throw new ModbusConnectionException($"TCP 连接失败: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancelToken = default) {
        if (_disposed) return;

        await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        try {
            await DisconnectInternalAsync().ConfigureAwait(false);
        } finally {
            _lock.Release();
        }
    }

    private async Task DisconnectInternalAsync() {
        try {
            if (_stream != null) {
                await _stream.FlushAsync().ConfigureAwait(false);
                _stream.Close();
                _stream = null;
            }

            _tcpClient?.Close();
            _tcpClient = null;
        } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException) { }
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (!IsConnected) throw new ModbusConnectionException("TCP 连接未建立");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        cts.CancelAfter(Timeout);

        await _lock.WaitAsync(cts.Token).ConfigureAwait(false);
        try {
            var stream = _stream!;

            // 发送请求
            await stream.WriteAsync(request, cts.Token).ConfigureAwait(false);
            await stream.FlushAsync(cts.Token).ConfigureAwait(false);

            // 接收响应
            return await ReceiveResponseAsync(cts.Token).ConfigureAwait(false);
        } catch (TimeoutException) {
            throw new ModbusTimeoutException($"TCP [{config.RemoteHost}:{config.RemotePort}] 通信超时");
        } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancelToken.IsCancellationRequested) {
            throw new ModbusTimeoutException($"TCP [{config.RemoteHost}:{config.RemotePort}] 通信超时，操作已取消");
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"TCP [{config.RemoteHost}:{config.RemotePort}] 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        // 创建一个足够大的缓冲区来接收数据
        var buffer = ArrayPool<byte>.Shared.Rent(1024);

        try {
            var stream = _stream!;

            var bytesRead = 0;
            var totalBytesRead = 0;

            // 先读取前几个字节来确定响应长度
            while (totalBytesRead < 6 && !cancelToken.IsCancellationRequested) {
                bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, 6 - totalBytesRead), cancelToken).ConfigureAwait(false);
                if (bytesRead == 0) {
                    throw new ModbusCommunicationException($"TCP [{config.RemoteHost}:{config.RemotePort}] 连接 意外关闭");
                }
                totalBytesRead += bytesRead;
            }

            // 根据协议类型确定完整的响应长度
            // 这里我们先尝试读取更多数据，直到没有更多数据或超时
            try {
                while (totalBytesRead < buffer.Length && !cancelToken.IsCancellationRequested) {
                    // 设置一个较短的超时时间来检测数据结束
                    using var quickCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
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

            if (totalBytesRead == 0) throw new ModbusTimeoutException($"TCP 接收超时，未收到响应数据");

            // 返回实际读取的数据
            var result = new byte[totalBytesRead];
            Array.Copy(buffer, 0, result, 0, totalBytesRead);
            return result;
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose() {
        if (_disposed) return;

        try {
            DisconnectInternalAsync().Wait(1000);
        } catch (Exception ex) when (ex is AggregateException || ex is ObjectDisposedException) { }
        _lock?.Dispose();

        _disposed = true;
    }

    public async ValueTask DisposeAsync() {
        if (_disposed) return;

        await DisconnectAsync().ConfigureAwait(false);
        _lock?.Dispose();

        _disposed = true;
    }
}