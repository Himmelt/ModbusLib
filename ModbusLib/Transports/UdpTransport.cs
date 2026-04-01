using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Transports;

/// <summary>
/// UDP传输实现
/// </summary>
public sealed class UdpTransport(NetworkConfig config) : IModbusTransport {

    private bool _disposed;
    private UdpClient? _udpClient;
    private IPEndPoint? _remoteEndPoint;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public int Timeout { get; set; } = -1;
    public bool IsConnected => _udpClient != null && _remoteEndPoint != null;

    public async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        try {
            if (IsConnected) return true;

            await DisconnectInternalAsync().ConfigureAwait(false);

            // 解析远程主机地址
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(config.RemoteHost), config.RemotePort);

            var localIP = string.IsNullOrWhiteSpace(config.LocalHost) ? IPAddress.Any : IPAddress.Parse(config.LocalHost);
            var localPort = config.LocalPort ?? 0;
            _udpClient = new UdpClient(new IPEndPoint(localIP, localPort));

            // 配置UDP选项
            _udpClient.Client.ReceiveTimeout = config.ReceiveTimeout;
            _udpClient.Client.SendTimeout = config.SendTimeout;
            _udpClient.Client.ReceiveBufferSize = config.ReceiveBufferSize;
            _udpClient.Client.SendBufferSize = config.SendBufferSize;

            // UDP是无连接协议，这里只是配置远程端点
            _udpClient.Connect(_remoteEndPoint);

            return true;
        } catch (Exception ex) {
            await DisconnectInternalAsync().ConfigureAwait(false);
            throw new ModbusConnectionException($"UDP 连接失败: {ex.Message}", ex);
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

    private Task DisconnectInternalAsync() {
        try {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
            _remoteEndPoint = null;
        } catch (SocketException) { }
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (!IsConnected) throw new ModbusConnectionException("UDP 连接未建立");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        cts.CancelAfter(Timeout);

        await _lock.WaitAsync(cts.Token).ConfigureAwait(false);
        try {
            var udpClient = _udpClient!;
            var remoteEndPoint = _remoteEndPoint!;

            // 发送请求
            var bytesSent = await udpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            if (bytesSent != request.Length) {
                throw new ModbusCommunicationException($"UDP 发送不完整，期望{request.Length}字节，实际发送{bytesSent}字节");
            }

            // 接收响应
            return await ReceiveResponseAsync(cts.Token).ConfigureAwait(false);
        } catch (TimeoutException) {
            throw new ModbusTimeoutException($"UDP [{config.RemoteHost}:{config.RemotePort}] 通信超时");
        } catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancelToken.IsCancellationRequested) {
            throw new ModbusTimeoutException($"UDP [{config.RemoteHost}:{config.RemotePort}] 通信超时，操作已取消");
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"UDP [{config.RemoteHost}:{config.RemotePort}] 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        var udpClient = _udpClient!;
        var result = await udpClient.ReceiveAsync(cancelToken).ConfigureAwait(false);
        return result.Buffer;
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