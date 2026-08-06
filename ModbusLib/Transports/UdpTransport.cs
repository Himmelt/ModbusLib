using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Utils;
using System.Net;
using System.Net.Sockets;
using ProtocolType = ModbusLib.Enums.ProtocolType;

namespace ModbusLib.Transports;

/// <summary>
/// UDP传输实现
/// </summary>
public sealed class UdpTransport : IModbusTransport {

    private readonly NetworkConfig _config;
    private readonly ProtocolType _protocol;
    private bool _disposed;
    private UdpClient? _udpClient;
    private IPEndPoint? _remoteEndPoint;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// 使用 TCP 协议帧（MBAP）创建传输
    /// </summary>
    public UdpTransport(NetworkConfig config) : this(config, ProtocolType.Tcp) { }

    /// <summary>
    /// 创建传输，<paramref name="protocol"/> 决定响应帧的校验方式（MBAP 或 RTU）
    /// </summary>
    public UdpTransport(NetworkConfig config, ProtocolType protocol) {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _protocol = protocol;
    }

    public int Timeout { get; set; } = -1;
    public bool IsConnected => _udpClient != null && _remoteEndPoint != null;

    public async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try {
            await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException($"UDP [{_config.RemoteHost}:{_config.RemotePort}] 获取通信锁超时");
        }

        try {
            if (IsConnected) return true;

            DisconnectInternal();

            // 解析远端地址，支持域名（与 README 承诺一致）
            var addresses = await Dns.GetHostAddressesAsync(_config.RemoteHost, cancelToken).ConfigureAwait(false);
            if (addresses.Length == 0) {
                throw new ModbusConnectionException($"UDP 无法解析主机名: {_config.RemoteHost}");
            }
            var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses[0];
            _remoteEndPoint = new IPEndPoint(address, _config.RemotePort);

            var localIP = string.IsNullOrWhiteSpace(_config.LocalHost) ? IPAddress.Any : IPAddress.Parse(_config.LocalHost);
            var localPort = _config.LocalPort ?? 0;
            _udpClient = new UdpClient(new IPEndPoint(localIP, localPort));

            _udpClient.Client.ReceiveTimeout = _config.ReceiveTimeout;
            _udpClient.Client.SendTimeout = _config.SendTimeout;
            _udpClient.Client.ReceiveBufferSize = _config.ReceiveBufferSize;
            _udpClient.Client.SendBufferSize = _config.SendBufferSize;

            // UDP 是无连接协议，这里仅配置远端端点
            _udpClient.Connect(_remoteEndPoint);

            return true;
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            DisconnectInternal();
            throw new ModbusTimeoutException($"UDP [{_config.RemoteHost}:{_config.RemotePort}] 连接超时");
        } catch (Exception ex) {
            DisconnectInternal();
            throw new ModbusConnectionException($"UDP 连接失败: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancelToken = default) {
        if (_disposed) return;

        try {
            await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            return;
        }
        try {
            DisconnectInternal();
        } finally {
            _lock.Release();
        }
    }

    private void DisconnectInternal() {
        try {
            _udpClient?.Close();
        } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException) { }
        _udpClient = null;
        _remoteEndPoint = null;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        using var opCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        if (Timeout >= 0) opCts.CancelAfter(Timeout);

        try {
            await _lock.WaitAsync(opCts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException($"UDP [{_config.RemoteHost}:{_config.RemotePort}] 获取通信锁超时");
        }

        try {
            if (!IsConnected) throw new ModbusConnectionException("UDP 连接未建立");

            var udpClient = _udpClient!;

            using (var sendCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.SendTimeout >= 0) sendCts.CancelAfter(_config.SendTimeout);
                var bytesSent = await udpClient.SendAsync(request, sendCts.Token).ConfigureAwait(false);
                if (bytesSent != request.Length) {
                    throw new ModbusCommunicationException($"UDP 发送不完整，期望 {request.Length} 字节，实际发送 {bytesSent} 字节");
                }
            }

            using (var recvCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.ReceiveTimeout >= 0) recvCts.CancelAfter(_config.ReceiveTimeout);
                return await ReceiveResponseAsync(recvCts.Token).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException($"UDP [{_config.RemoteHost}:{_config.RemotePort}] 通信超时，操作已取消");
        } catch (ModbusConnectionException) {
            throw;
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"UDP [{_config.RemoteHost}:{_config.RemotePort}] 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        var udpClient = _udpClient!;
        var remoteEndPoint = _remoteEndPoint!;
        var result = await udpClient.ReceiveAsync(cancelToken).ConfigureAwait(false);

        // 校验响应来源，避免将无关数据包当作响应
        if (!remoteEndPoint.Equals(result.RemoteEndPoint)) {
            throw new ModbusCommunicationException($"UDP 响应来源不匹配: 期望 {remoteEndPoint}, 实际 {result.RemoteEndPoint}");
        }

        var buffer = result.Buffer;
        if (_protocol == ProtocolType.Tcp) {
            if (buffer.Length < 6) {
                throw new ModbusCommunicationException("UDP 响应数据不足（缺少MBAP头部）");
            }
            var protocolId = (buffer[2] << 8) | buffer[3];
            if (protocolId != 0) {
                throw new ModbusCommunicationException($"UDP 响应协议ID无效: 0x{protocolId:X4}");
            }
            var length = (buffer[4] << 8) | buffer[5];
            if (6 + length != buffer.Length) {
                throw new ModbusCommunicationException($"UDP 响应长度与MBAP长度不符: 数据包{buffer.Length}字节, MBAP声明{6 + length}字节");
            }
        } else if (buffer.Length >= 5 && !Crc16Utils.ValidateCrc16(buffer)) {
            throw new ModbusCommunicationException("UDP 响应CRC校验失败");
        }

        return buffer;
    }

    public void Dispose() {
        if (_disposed) return;

        DisconnectInternal();
        _lock.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync() {
        if (_disposed) return;

        DisconnectInternal();
        _lock.Dispose();
        _disposed = true;

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
