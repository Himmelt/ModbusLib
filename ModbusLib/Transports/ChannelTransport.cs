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

    public Task DisconnectAsync(CancellationToken cancelToken = default) {
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        if (Timeout >= 0) cts.CancelAfter(Timeout);

        try {
            await _lock.WaitAsync(cts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException("Channel 获取通信锁超时");
        }

        try {
            await _session.ClientToServer.Writer.WriteAsync(request, cts.Token).ConfigureAwait(false);
            return await ReceiveResponseAsync(cts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
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
    /// 持续读取直到收到一帧完整响应（按 MBAP 长度或 RTU 字节计数确定帧长）。
    /// </summary>
    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
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

            frameLength ??= ModbusFrameParser.TryGetResponseFrameLength(
                CollectionsMarshal.AsSpan(response), _protocol);

            if (frameLength is int expected && response.Count >= expected) {
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
