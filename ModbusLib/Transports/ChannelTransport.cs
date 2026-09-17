using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Utils;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace ModbusLib.Transports;

public sealed class ChannelTransport : IModbusTransport {

    private readonly ChannelSession _session;
    private readonly ProtocolType _protocol;
    private bool _disposed;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// 使用 TCP 协议帧（MBAP）创建传输
    /// </summary>
    public ChannelTransport(ChannelSession session) : this(session, ProtocolType.Tcp) { }

    /// <summary>
    /// 创建传输，<paramref name="protocol"/> 决定响应帧的解析方式（MBAP 或 RTU）
    /// </summary>
    public ChannelTransport(ChannelSession session, ProtocolType protocol) {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _protocol = protocol;
    }

    public int Timeout { get; set; } = -1;
    public bool IsConnected => !_disposed;

    public Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(true);
    }

    /// <summary>
    /// 复位会话缓冲。Channel 传输没有物理连接可断开，因此这里的“断开”语义为：
    /// 丢弃 <see cref="ChannelSession.ServerToClient"/> 中所有滞留帧，使下一次请求从干净状态开始配对。
    /// 这样客户端基类的“断开 + 重连”式重试在本传输上才真正具备自愈能力。
    /// </summary>
    /// <remarks>
    /// 该方法不获取通信锁，以便在请求卡住时也能立即复位。因此若在某个请求进行中调用，
    /// 会连带丢弃该请求的响应（该请求随即超时）——这是“复位”的应有语义；
    /// 在请求失败后调用（客户端基类重试的路径）不存在此影响。
    /// </remarks>
    public Task DisconnectAsync(CancellationToken cancelToken = default) {
        DiscardStaleResponses();
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        if (Timeout >= 0) cts.CancelAfter(Timeout);

        try {
            await _lock.WaitAsync(cts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException("Channel 获取通信锁超时");
        }

        // 期望的响应身份：RTU 无事务标识，只能用 设备地址 + 功能码 + 帧长 做相关性校验。
        // MBAP 有事务号、协议层已校验且可经重试自愈，因此不在此加严，避免把明确的报错退化成超时。
        var expectation = _protocol == ProtocolType.Rtu
            ? ModbusFrameParser.TryGetRtuResponseExpectation(request)
            : null;

        try {
            // 发请求前排空：本传输严格串行（由 _lock 保证），因此此刻通道里存在的任何帧，
            // 必然属于已经放弃的历史事务（上一轮超时后的迟到响应等），一律丢弃，
            // 保证“本次请求”与“读到的那一帧”之间通道是干净的。
            DiscardStaleResponses();

            await _session.ClientToServer.Writer.WriteAsync(request, cts.Token).ConfigureAwait(false);
            return await ReceiveResponseAsync(expectation, cts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
            // 超时：尽力丢弃本轮已落地的迟到响应，缩小污染下一轮的窗口
            DiscardStaleResponses();
            throw new ModbusTimeoutException("Channel 通信超时，操作已取消");
        } catch (ModbusCommunicationException) {
            throw;
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"Channel 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    /// <summary>
    /// 丢弃通道中所有已到达、尚未被消费的帧（TryRead 直到读空）。
    /// </summary>
    private void DiscardStaleResponses() {
        while (_session.ServerToClient.Reader.TryRead(out _)) {
            // 仅丢弃：这些都是无人认领的历史帧
        }
    }

    /// <summary>
    /// 持续读取直到收到一帧完整响应（按 MBAP 长度或 RTU 字节计数确定帧长）。
    /// RTU 下帧收全后还要校验其身份是否与本次请求对应，不对应的帧（上一轮迟到 / 对端错配 / 流水线推送）
    /// 直接丢弃并继续等待，直到总超时。
    /// </summary>
    private async Task<byte[]> ReceiveResponseAsync(RtuResponseExpectation? expectation, CancellationToken cancelToken) {
        var response = new List<byte>(32);
        int? frameLength = null;

        while (true) {
            byte[] item;
            try {
                item = await _session.ServerToClient.Reader.ReadAsync(cancelToken).ConfigureAwait(false);
            } catch (ChannelClosedException) {
                throw new ModbusCommunicationException("Channel 连接已关闭，未收到响应");
            }

            response.AddRange(item);

            // 帧起始位置校验：刚读完的这一段若位于帧首，其首字节必须符合本次请求的身份，
            // 否则是上一轮超时遗留的半帧尾部等碎片，直接丢弃（绝不拿它去推算帧长）
            if (expectation is { } startExpect && response.Count == item.Length &&
                !startExpect.CouldStartFrame(CollectionsMarshal.AsSpan(response))) {
                response.Clear();
                frameLength = null;
                continue;
            }

            frameLength ??= ModbusFrameParser.TryGetResponseFrameLength(
                CollectionsMarshal.AsSpan(response), _protocol);

            if (frameLength is int expected && response.Count >= expected) {
                if (expectation is { } expect &&
                    !expect.Matches(CollectionsMarshal.AsSpan(response)[..expected])) {
                    // 与本轮请求不相关的帧：丢弃后继续等真正的响应（仍在同一个总超时内）
                    response.Clear();
                    frameLength = null;
                    continue;
                }

                // 只返回这一帧：同一批次里被顺带读到的后续字节属于无人认领的帧，不参与本次解析
                if (response.Count > expected) response.RemoveRange(expected, response.Count - expected);
                break;
            }

            // 通道已完成且当前数据不足以构成完整帧
            if (_session.ServerToClient.Reader.Completion.IsCompleted) {
                throw new ModbusCommunicationException("Channel 响应数据不完整");
            }
        }

        if (response.Count == 0) throw new ModbusTimeoutException("Channel 接收超时，未收到响应数据");
        return [.. response];
    }

    public void Dispose() {
        if (_disposed) return;
        _lock?.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
