namespace ModbusLib.Exceptions;

/// <summary>
/// Modbus 通信异常
/// </summary>
public class ModbusCommunicationException : Exception {

    public ModbusCommunicationException() { }

    public ModbusCommunicationException(string message) : base(message) { }

    public ModbusCommunicationException(string message, Exception innerEx) : base(message, innerEx) { }
}

/// <summary>
/// Modbus 连接异常
/// </summary>
public class ModbusConnectionException : Exception {

    public ModbusConnectionException() { }

    public ModbusConnectionException(string message) : base(message) { }

    public ModbusConnectionException(string message, Exception innerEx) : base(message, innerEx) { }
}

/// <summary>
/// Modbus 超时异常
/// </summary>
public class ModbusTimeoutException : Exception {

    public ModbusTimeoutException() { }

    public ModbusTimeoutException(string message) : base(message) { }

    public ModbusTimeoutException(string message, Exception innerEx) : base(message, innerEx) { }
}
