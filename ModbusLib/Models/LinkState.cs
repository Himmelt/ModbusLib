namespace ModbusLib.Models;

/// <summary>
/// 链路状态。与 <see cref="Interfaces.IModbusTransport.IsConnected"/>（"管子通不通"）不同，
/// 本状态描述的是**传输层自己认定的**链路健康程度，由传输层在检测到明确证据时翻转。
/// </summary>
public enum LinkState {
    /// <summary>尚未建立连接，状态未知。</summary>
    Unknown,

    /// <summary>链路可用：已建立，且未检测到任何断开证据。</summary>
    Up,

    /// <summary>链路已断开：收到 FIN/RST、IO 异常或显式断开。</summary>
    Down
}

/// <summary>
/// 链路状态变更事件参数。<see cref="Reason"/> 供日志使用，不应参与业务判断。
/// </summary>
public sealed class LinkStateChangedEventArgs(LinkState state, string reason) : EventArgs {
    public LinkState State { get; } = state;

    public string Reason { get; } = reason;
}
