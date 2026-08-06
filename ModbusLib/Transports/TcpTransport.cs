using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using ProtocolType = ModbusLib.Enums.ProtocolType;

namespace ModbusLib.Transports;

/// <summary>
/// TCP传输实现
/// </summary>
public sealed class TcpTransport : IModbusTransport {

    private readonly NetworkConfig _config;
    private readonly ProtocolType _protocol;
    private bool _disposed;
    private bool _connected;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// 使用 TCP 协议帧（MBAP）创建传输
    /// </summary>
    public TcpTransport(NetworkConfig config) : this(config, ProtocolType.Tcp) { }

    /// <summary>
    /// 创建传输，<paramref name="protocol"/> 决定响应帧的解析方式（MBAP 或 RTU）
    /// </summary>
    public TcpTransport(NetworkConfig config, ProtocolType protocol) {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _protocol = protocol;
    }

    public int Timeout { get; set; } = -1;

    public bool IsConnected {
        get {
            if (!_connected || _tcpClient is null || _stream is null) return false;
            try {
                var socket = _tcpClient.Client;
                if (socket is null) return false;
                // 轮询探测对端是否已经关闭连接（有可读数据但缓冲区为空 => 连接已关闭）
                return !(socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0);
            } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException) {
                return false;
            }
        }
    }

    public async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        try {
            if (IsConnected) return true;

            DisconnectInternal();

            var localIP = string.IsNullOrWhiteSpace(_config.LocalHost) ? IPAddress.Any : IPAddress.Parse(_config.LocalHost);
            var localPort = _config.LocalPort ?? 0;
            _tcpClient = new TcpClient(new IPEndPoint(localIP, localPort)) {
                ReceiveTimeout = _config.ReceiveTimeout,
                SendTimeout = _config.SendTimeout,
                ReceiveBufferSize = _config.ReceiveBufferSize,
                SendBufferSize = _config.SendBufferSize
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            if (_config.ConnectTimeout >= 0) cts.CancelAfter(_config.ConnectTimeout);

            await _tcpClient.ConnectAsync(_config.RemoteHost, _config.RemotePort, cts.Token).ConfigureAwait(false);

            if (_tcpClient.Client != null) {
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }

            _stream = _tcpClient.GetStream();
            _stream.ReadTimeout = _config.ReceiveTimeout;
            _stream.WriteTimeout = _config.SendTimeout;
            _connected = true;

            return true;
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
            DisconnectInternal();
            throw new ModbusTimeoutException($"TCP [{_config.RemoteHost}:{_config.RemotePort}] 连接超时");
        } catch (Exception ex) {
            DisconnectInternal();
            throw new ModbusConnectionException($"TCP 连接失败: {ex.Message}", ex);
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

    /// <summary>
    /// 同步关闭底层连接。在超时或通信异常时调用，确保清空 socket 中可能残留的半帧数据，
    /// 避免后续请求读到错位数据。
    /// </summary>
    private void DisconnectInternal() {
        _connected = false;
        try {
            _stream?.Close();
        } catch (Exception ex) when (ex is SocketException || ex is IOException || ex is ObjectDisposedException) { }
        _stream = null;

        try {
            _tcpClient?.Close();
        } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException) { }
        _tcpClient = null;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // 整体操作超时
        using var opCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        if (Timeout >= 0) opCts.CancelAfter(Timeout);

        try {
            await _lock.WaitAsync(opCts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException($"TCP [{_config.RemoteHost}:{_config.RemotePort}] 获取通信锁超时");
        }

        try {
            if (!IsConnected) throw new ModbusConnectionException("TCP 连接未建立");

            var stream = _stream!;

            // 发送阶段（受 SendTimeout 限制）
            using (var sendCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.SendTimeout >= 0) sendCts.CancelAfter(_config.SendTimeout);
                await stream.WriteAsync(request, sendCts.Token).ConfigureAwait(false);
            }

            // 接收阶段（受 ReceiveTimeout 限制）
            using (var recvCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.ReceiveTimeout >= 0) recvCts.CancelAfter(_config.ReceiveTimeout);
                return await ReceiveResponseAsync(recvCts.Token).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
            // 自身超时：断开连接，清空可能残留的半帧数据
            DisconnectInternal();
            throw new ModbusTimeoutException($"TCP [{_config.RemoteHost}:{_config.RemotePort}] 通信超时，操作已取消");
        } catch (ModbusConnectionException) {
            throw;
        } catch (Exception ex) {
            // 网络异常：标记连接失效，便于上层重连后自愈
            DisconnectInternal();
            throw new ModbusCommunicationException($"TCP [{_config.RemoteHost}:{_config.RemotePort}] 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    /// <summary>
    /// 按协议精确读取一帧完整响应：
    /// TCP 协议依据 MBAP 长度字段，RTU 协议依据功能码/字节计数字段，
    /// 不再使用固定 100ms 探测循环。
    /// </summary>
    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try {
            var stream = _stream!;
            var total = 0;
            int frameLength;

            if (_protocol == ProtocolType.Tcp) {
                // MBAP 帧：先读取 6 字节头部，再依据 length 字段读取剩余部分
                total = await ReadAtLeastAsync(stream, buffer, 0, 6, cancelToken).ConfigureAwait(false);
                var length = (buffer[4] << 8) | buffer[5];
                frameLength = 6 + length;
                EnsureBufferCapacity(ref buffer, frameLength);
                total = await ReadAtLeastAsync(stream, buffer, total, frameLength - total, cancelToken).ConfigureAwait(false);
                return CopyFrame(buffer, frameLength);
            }

            // RTU 帧：先读取 设备地址 + 功能码，再依据功能码确定帧长度
            total = await ReadAtLeastAsync(stream, buffer, 0, 2, cancelToken).ConfigureAwait(false);
            var functionCode = buffer[1];
            if ((functionCode & 0x80) != 0) {
                // 异常响应: 设备地址 + 功能码(异常) + 异常码 + CRC
                frameLength = 5;
            } else if (functionCode is (byte)ModbusFunction.ReadCoils or (byte)ModbusFunction.ReadDiscreteInputs
                or (byte)ModbusFunction.ReadHoldingRegisters or (byte)ModbusFunction.ReadInputRegisters
                or (byte)ModbusFunction.ReadWriteMultipleRegisters) {
                // 读响应: 设备地址 + 功能码 + 字节数 + 数据 + CRC
                total = await ReadAtLeastAsync(stream, buffer, total, 3 - total, cancelToken).ConfigureAwait(false);
                frameLength = 5 + buffer[2];
            } else if (functionCode is (byte)ModbusFunction.WriteSingleCoil or (byte)ModbusFunction.WriteSingleRegister
                or (byte)ModbusFunction.WriteMultipleCoils or (byte)ModbusFunction.WriteMultipleRegisters) {
                // 写响应: 回显请求（8字节）
                frameLength = 8;
            } else {
                throw new ModbusCommunicationException($"无法确定RTU响应帧长度，功能码: 0x{functionCode:X2}");
            }

            EnsureBufferCapacity(ref buffer, frameLength);
            total = await ReadAtLeastAsync(stream, buffer, total, frameLength - total, cancelToken).ConfigureAwait(false);
            return CopyFrame(buffer, frameLength);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<int> ReadAtLeastAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancelToken) {
        var total = 0;
        while (total < count) {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset + total, count - total), cancelToken).ConfigureAwait(false);
            if (bytesRead == 0) {
                throw new ModbusCommunicationException($"TCP 连接被对端意外关闭");
            }
            total += bytesRead;
        }
        // 返回累计已读取字节数（offset + count）
        return offset + total;
    }

    private static void EnsureBufferCapacity(ref byte[] buffer, int required) {
        if (buffer.Length >= required) return;

        var newBuffer = ArrayPool<byte>.Shared.Rent(required);
        Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    private static byte[] CopyFrame(byte[] buffer, int length) {
        var result = new byte[length];
        Array.Copy(buffer, 0, result, 0, length);
        return result;
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
