using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Tests.Mocks;

/// <summary>
/// 传输层测试替身：模拟 <c>SerialTransport</c> 的失败语义——**通信失败后不把连接置为断开**
/// （TCP/UDP 传输失败时会自行 <c>DisconnectInternal</c>，串口不会），只有显式重建连接才恢复链路。
/// 用于验证：当 <c>IsConnected</c> 仍为 <c>true</c> 时循环内的重建不会触发，
/// 重试路径上的强制重连是唯一的恢复手段。
/// </summary>
public sealed class StaleLinkTransport : IModbusTransport {

    private bool _open;

    /// <summary>链路是否已失效：为 <c>true</c> 时发送一律抛超时，但 <see cref="IsConnected"/> 仍报 <c>true</c>。</summary>
    public bool LinkIsStale { get; set; }

    /// <summary>累计 <c>ConnectAsync</c> 调用次数（含重建）。</summary>
    public int ConnectCalls { get; private set; }

    /// <summary>累计 <c>SendReceiveAsync</c> 调用次数。</summary>
    public int SendCalls { get; private set; }

    /// <summary>模拟"链路明确断开"：用于验证客户端不再重发、且不做隐式重连。</summary>
    public void ForceLinkDown() => LinkState = LinkState.Down;

    /// <summary>只让接下来 N 次发送失败（模拟偶发丢包），之后恢复。链路状态保持 Up。</summary>
    public int FailNextSends { get; set; }

    public int Timeout { get; set; } = -1;

    /// <summary>与 <c>SerialTransport.IsConnected</c> 一致：只看底层是否打开，不反映链路是否已失效。</summary>
    public bool IsConnected => _open;

    /// <summary>与 SerialTransport 一致：通信失败不改变链路状态。</summary>
    public LinkState LinkState { get; private set; } = LinkState.Unknown;

    public event EventHandler<LinkStateChangedEventArgs>? LinkStateChanged { add { } remove { } }

    public Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        _open = true;
        LinkIsStale = false;          // 重建连接 = 链路恢复
        LinkState = LinkState.Up;
        ConnectCalls++;
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken cancelToken = default) {
        _open = false;
        LinkState = LinkState.Down;
        return Task.CompletedTask;
    }

    public Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        SendCalls++;
        if (FailNextSends > 0) {
            FailNextSends--;
            throw new ModbusTimeoutException("stub：本次偶发丢包（链路仍健康）");
        }
        if (LinkIsStale) {
            // 关键：失败但不自断连接
            throw new ModbusTimeoutException("stub：链路已失效（超时），但 IsConnected 仍为 true");
        }
        return Task.FromResult(new byte[] { 5, 0x01, 0x03, 0x04, 0x10 });
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
