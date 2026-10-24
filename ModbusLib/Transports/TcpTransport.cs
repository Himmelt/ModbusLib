using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Utils;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using ProtocolType = ModbusLib.Enums.ProtocolType;

namespace ModbusLib.Transports;

/// <summary>
/// TCP传输实现
/// </summary>
/// <remarks>
/// <para><b>故障分层</b>：本传输把"链路"与"从站"分开看待。</para>
/// <para>
/// · 链路级（收到 FIN / RST / IO 异常）：关闭 socket，<see cref="LinkState"/> 翻转为
///   <see cref="LinkState.Down"/> 并通知订阅者，由调用方决定何时重连。
/// </para>
/// <para>
/// · 从站级（请求发得出去、只是没等到响应）：<b>不动连接</b>。这条连接可能还被其它设备共用，
///   一个从站不应答就拆掉整条链路，会让同链路的其它设备一起停摆。此时只清空接收缓冲里的
///   迟到字节（<see cref="DrainInput"/>），抛 <see cref="ModbusTimeoutException"/>。
/// </para>
/// <para>库只负责报告状态与证据，不隐式重建连接——重连是调用方（连接看护者）的策略。</para>
/// </remarks>
public sealed class TcpTransport : IModbusTransport {
    private readonly NetworkConfig _config;
    private readonly ProtocolType _protocol;
    private bool _disposed;
    private bool _connected;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly object _eventLock = new();
    private LinkStateChangedEventArgs? _pendingEvent;
    private LinkState _linkState = LinkState.Unknown;

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
                // 轮询探测对端是否已经关闭连接（有可读数据但缓冲区为空 => 连接已关闭）。
                // 这是**唯一能在没有流量时拿到的链路级证据**：一旦观察到，就地清理并通知，
                // 否则 LinkState 会一直停在 Up，看护者永远等不到断开事件。
                if (socket.Poll(0, SelectMode.SelectRead) && socket.Available == 0) {
                    DisconnectInternal("对端已关闭连接");
                    FlushLinkEvents();     // 本路径不持锁，可以立即通知
                    return false;
                }
                return true;
            } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException) {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public LinkState LinkState => _linkState;

    /// <inheritdoc />
    public event EventHandler<LinkStateChangedEventArgs>? LinkStateChanged;

    public async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        try {
            if (IsConnected) return true;

            DisconnectInternal(null);

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
                ApplyKeepAlive(_tcpClient.Client, _config);
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, _config.KeepAliveEnabled);
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }

            _stream = _tcpClient.GetStream();
            _connected = true;
            SetLinkState(LinkState.Up, "连接已建立");
            return true;
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            DisconnectInternal("连接超时");
            throw new ModbusTimeoutException($"TCP [{_config.RemoteHost}:{_config.RemotePort}] 连接超时");
        } catch (Exception ex) {
            DisconnectInternal($"连接失败: {ex.Message}");
            throw new ModbusConnectionException($"TCP 连接失败: {ex.Message}", ex);
        } finally {
            _lock.Release();
            FlushLinkEvents();
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
            DisconnectInternal("主动断开");
        } finally {
            _lock.Release();
            FlushLinkEvents();
        }
    }

    /// <summary>
    /// 同步关闭底层连接，并把链路状态标记为 <see cref="LinkState.Down"/>（事件延后到锁外发送）。
    /// 只在**链路级故障**或显式断开时调用；请求超时不调用本方法。
    /// </summary>
    private void DisconnectInternal(string? reason) {
        _connected = false;
        SetLinkState(LinkState.Down, reason ?? "连接已关闭");

        try { _stream?.Close(); } catch (Exception ex) when (ex is SocketException || ex is IOException || ex is ObjectDisposedException) { }
        _stream = null;

        try { _tcpClient?.Close(); } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException) { }
        _tcpClient = null;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // ① 等通信锁：独立预算。排队只说明"这条链路还被别人占着"，与对端回没回无关。
        var lockTimeout = _config.LockTimeout >= 0 ? _config.LockTimeout : Timeout;
        using var lockCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        if (lockTimeout >= 0) lockCts.CancelAfter(lockTimeout);
        try {
            await _lock.WaitAsync(lockCts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException(
                $"TCP [{_config.RemoteHost}:{_config.RemotePort}] 等待通信锁超时") { IsLockWaitTimeout = true };
        }

        // ② 拿到锁之后，本次事务（发送+接收）才有自己的预算
        using var opCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        if (Timeout >= 0) opCts.CancelAfter(Timeout);

        try {
            if (!IsConnected) {
                throw new ModbusConnectionException("TCP 连接未建立") { IsLinkBroken = true };
            }

            var stream = _stream!;

            // 发送前先排空：清掉上一轮迟到响应/半帧，保证本次读到的第一字节属于本轮
            DrainInput();

            // 响应身份校验：总线被多个主站共用时，响应里可能混进别人的帧，不能拿到什么就当什么
            var expectation = _protocol == ProtocolType.Rtu
                ? ModbusFrameParser.TryGetRtuResponseExpectation(request)
                : null;

            // 发送阶段（受 SendTimeout 限制）
            using (var sendCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.SendTimeout >= 0) sendCts.CancelAfter(_config.SendTimeout);
                await stream.WriteAsync(request, sendCts.Token).ConfigureAwait(false);
            }

            // 接收阶段（受 ReceiveTimeout 限制）
            using (var recvCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.ReceiveTimeout >= 0) recvCts.CancelAfter(_config.ReceiveTimeout);
                return await ReceiveResponseAsync(expectation, recvCts.Token).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (ModbusConnectionException) {
            throw; // 已标记为链路级
        } catch (ModbusCommunicationException ex) when (ex.IsLinkBroken) {
            DisconnectInternal("对端已关闭连接");   // 链路级异常必须落地为连接关闭
            throw;
        } catch (OperationCanceledException) {
            // 超时 ≠ 链路故障：连接保留，供同链路其它设备继续使用；只清掉迟到字节
            DrainInput();
            throw new ModbusTimeoutException($"TCP [{_config.RemoteHost}:{_config.RemotePort}] 通信超时，未收到响应");
        } catch (Exception ex) {
            // 网络异常：链路确实坏了，关闭连接并通知订阅者
            DisconnectInternal($"通信异常: {ex.Message}");
            throw new ModbusCommunicationException(
                $"TCP [{_config.RemoteHost}:{_config.RemotePort}] 通信异常: {ex.Message}", ex) { IsLinkBroken = true };
        } finally {
            _lock.Release();
            FlushLinkEvents();
        }
    }

    /// <summary>
    /// 按协议精确读取一帧完整响应：
    /// TCP 协议依据 MBAP 长度字段，RTU 协议依据功能码/字节计数字段。
    /// <para>
    /// RTU 下还会用 <paramref name="expectation"/> 校验帧身份：读到的帧若不是本次请求的响应
    /// （上一轮迟到 / 总线上别的从站在答话），丢弃后继续等，直到总超时。
    /// </para>
    /// </summary>
    private async Task<byte[]> ReceiveResponseAsync(RtuResponseExpectation? expectation, CancellationToken cancelToken) {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try {
            var stream = _stream!;
            int total;

            while (true) {
                if (_protocol == ProtocolType.Tcp) {
                    // MBAP 帧：先读取 6 字节头部，再依据 length 字段读取剩余部分
                    total = await ReadAtLeastAsync(stream, buffer, 0, 6, cancelToken).ConfigureAwait(false);
                    var length = (buffer[4] << 8) | buffer[5];
                    var mbapFrameLength = 6 + length;
                    EnsureBufferCapacity(ref buffer, mbapFrameLength);
                    total = await ReadAtLeastAsync(stream, buffer, total, mbapFrameLength - total, cancelToken).ConfigureAwait(false);
                    return CopyFrame(buffer, mbapFrameLength);
                }

                // RTU：先读 设备地址 + 功能码
                total = await ReadAtLeastAsync(stream, buffer, 0, 2, cancelToken).ConfigureAwait(false);

                // 帧首身份校验：首字节必须是本次请求的从站地址，否则是上一轮残留的半帧尾部
                if (expectation is { } startExpect && !startExpect.CouldStartFrame(buffer.AsSpan(0, 2))) {
                    DrainInput();
                    continue;
                }

                var functionCode = buffer[1];
                int frameLength;
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
                var frame = CopyFrame(buffer, frameLength);

                // 帧完整但身份不符：不是本次请求的响应，丢弃继续等
                if (expectation is { } frameExpect && !frameExpect.Matches(frame)) continue;

                return frame;
            }
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<int> ReadAtLeastAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancelToken) {
        var total = 0;
        while (total < count) {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset + total, count - total), cancelToken).ConfigureAwait(false);
            if (bytesRead == 0) {
                throw new ModbusCommunicationException("TCP 连接被对端意外关闭") { IsLinkBroken = true };
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

    /// <summary>
    /// 非阻塞读空接收缓冲，丢弃已经到达的迟到帧/半帧。等价于 libmodbus 的
    /// <c>MODBUS_ERROR_RECOVERY_PROTOCOL</c>：只清数据，不动连接。
    /// </summary>
    private void DrainInput() {
        try {
            var socket = _tcpClient?.Client;
            if (socket is null) return;
            var scratch = new byte[256];
            while (socket.Poll(0, SelectMode.SelectRead) && socket.Available > 0) {
                if (socket.Receive(scratch) <= 0) break;
            }
        } catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException) {
            DisconnectInternal("接收缓冲排空失败");
        }
    }

    /// <summary>
    /// 应用 KeepAlive 参数。系统默认 2 小时静默对故障发现毫无帮助，这里默认 30s/5s/3。
    /// 平台不支持细分参数时退回系统默认，不影响连接本身。
    /// </summary>
    private static void ApplyKeepAlive(Socket socket, NetworkConfig config) {
        if (!config.KeepAliveEnabled) return;
        try {
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, config.KeepAliveTimeSeconds);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, config.KeepAliveIntervalSeconds);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, config.KeepAliveRetryCount);
        } catch (Exception ex) when (ex is SocketException || ex is PlatformNotSupportedException || ex is ObjectDisposedException) {
            // 平台不支持细分参数：保持系统默认，KeepAlive 开关本身已在调用方处生效
        }
    }

    private void SetLinkState(LinkState state, string reason) {
        if (_linkState == state) return;
        _linkState = state;
        lock (_eventLock) {
            _pendingEvent = new LinkStateChangedEventArgs(state, reason);
        }
    }

    /// <summary>
    /// 发送挂起的状态变更事件。必须在**不持有通信锁**的上下文调用，
    /// 否则订阅者若回调 <see cref="ConnectAsync"/> 会自锁。
    /// </summary>
    private void FlushLinkEvents() {
        LinkStateChangedEventArgs? args;
        lock (_eventLock) {
            args = _pendingEvent;
            _pendingEvent = null;
        }
        if (args is null) return;
#pragma warning disable CA1031 // 有意吞掉：订阅者异常不得影响通信主路径
        try {
            LinkStateChanged?.Invoke(this, args);
        } catch {
        }
#pragma warning restore CA1031
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;

        DisconnectInternal("连接已释放");
        _lock.Dispose();
        FlushLinkEvents();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync() {
        Dispose();
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
