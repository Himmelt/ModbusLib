namespace ModbusLib.Exceptions;

/// <summary>
/// Modbus 通信异常
/// </summary>
public class ModbusCommunicationException : Exception {

    public ModbusCommunicationException() { }

    public ModbusCommunicationException(string message) : base(message) { }

    public ModbusCommunicationException(string message, Exception innerEx) : base(message, innerEx) { }

    /// <summary>
    /// 本次失败是否意味着**链路已断**（收到 FIN/RST、IO 异常等）。
    /// <para>
    /// false 表示链路仍然可用，只是这一次没拿到有效响应（对端超时、帧错配等）——
    /// 此时调用方不应重建连接，尤其是这条连接被多个设备共享时。
    /// </para>
    /// </summary>
    public bool IsLinkBroken { get; init; }
}

/// <summary>
/// Modbus 连接异常
/// </summary>
public class ModbusConnectionException : Exception {

    public ModbusConnectionException() { }

    public ModbusConnectionException(string message) : base(message) { }

    public ModbusConnectionException(string message, Exception innerEx) : base(message, innerEx) { }

    /// <summary>连接未建立或已断开，属于链路级故障。</summary>
    public bool IsLinkBroken { get; init; } = true;
}

/// <summary>
/// Modbus 超时异常
/// </summary>
public class ModbusTimeoutException : Exception {

    public ModbusTimeoutException() { }

    public ModbusTimeoutException(string message) : base(message) { }

    public ModbusTimeoutException(string message, Exception innerEx) : base(message, innerEx) { }

    /// <summary>
    /// 是否等通信锁超时（而非收发超时）。等锁超时说明这条链路还被其它请求占用，
    /// 与"对端没回"是两回事，调用方可以据此分别处置。
    /// </summary>
    public bool IsLockWaitTimeout { get; init; }
}
