using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Utils;
using System.Runtime.InteropServices;

namespace ModbusLib.Transports;

public sealed class PipeTransport : IModbusTransport {

    private readonly PipeSession _session;
    private readonly ProtocolType _protocol;
    private bool _disposed;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// 使用 TCP 协议帧（MBAP）创建传输
    /// </summary>
    public PipeTransport(PipeSession session) : this(session, ProtocolType.Tcp) { }

    /// <summary>
    /// 创建传输，<paramref name="protocol"/> 决定响应帧的解析方式（MBAP 或 RTU）
    /// </summary>
    public PipeTransport(PipeSession session, ProtocolType protocol) {
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
            throw new ModbusTimeoutException("Pipe 获取通信锁超时");
        }

        try {
            await _session.ClientToServer.Writer.WriteAsync(request, cts.Token).ConfigureAwait(false);
            await _session.ClientToServer.Writer.FlushAsync(cts.Token).ConfigureAwait(false);
            return await ReceiveResponseAsync(cts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException("Pipe 通信超时，操作已取消");
        } catch (ModbusCommunicationException) {
            throw;
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"Pipe 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    /// <summary>
    /// 持续读取直到收到一帧完整响应（按 MBAP 长度或 RTU 字节计数确定帧长），
    /// 不再只读取一次 PipeReader 返回的当前可用数据。
    /// </summary>
    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        var response = new List<byte>(32);
        int? frameLength = null;

        while (true) {
            var result = await _session.ServerToClient.Reader.ReadAsync(cancelToken).ConfigureAwait(false);
            if (result.IsCompleted && result.Buffer.IsEmpty) {
                throw new ModbusCommunicationException("Pipe 连接已关闭，未收到响应");
            }

            var bytesBefore = response.Count;
            foreach (var segment in result.Buffer) {
                response.AddRange(segment.ToArray());
            }

            frameLength ??= ModbusFrameParser.TryGetResponseFrameLength(
                CollectionsMarshal.AsSpan(response), _protocol);

            if (frameLength is int expected && response.Count >= expected) {
                // 只消费本帧数据，多余字节保留给后续请求
                if (response.Count == expected) {
                    _session.ServerToClient.Reader.AdvanceTo(result.Buffer.End);
                } else {
                    var consumed = result.Buffer.GetPosition(expected - bytesBefore);
                    _session.ServerToClient.Reader.AdvanceTo(consumed);
                }
                break;
            }

            if (result.IsCompleted) {
                // 对端已完成写入但帧不完整
                _session.ServerToClient.Reader.AdvanceTo(result.Buffer.End);
                throw new ModbusCommunicationException("Pipe 响应数据不完整");
            }

            _session.ServerToClient.Reader.AdvanceTo(result.Buffer.End);
        }

        if (response.Count == 0) throw new ModbusTimeoutException("Pipe 接收超时，未收到响应数据");
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
