using ModbusLib.Enums;

namespace ModbusLib.Exceptions;

/// <summary>
/// Modbus 异常基类
/// </summary>
public class ModbusException : Exception
{
    /// <summary>
    /// 异常码
    /// </summary>
    public ModbusExceptionCode ExceptionCode { get; }

    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; }

    /// <summary>
    /// 功能码
    /// </summary>
    public ModbusFunction Function { get; }

    public ModbusException(ModbusExceptionCode exceptionCode, byte slaveId, ModbusFunction function)
        : base($"Modbus异常: 从站{slaveId}, 功能码{(byte)function:X2}, 异常码{(byte)exceptionCode}")
    {
        ExceptionCode = exceptionCode;
        SlaveId = slaveId;
        Function = function;
    }

    public ModbusException(ModbusExceptionCode exceptionCode, byte slaveId, ModbusFunction function, string message)
        : base(message)
    {
        ExceptionCode = exceptionCode;
        SlaveId = slaveId;
        Function = function;
    }

    public ModbusException(ModbusExceptionCode exceptionCode, byte slaveId, ModbusFunction function, string message, Exception innerException)
        : base(message, innerException)
    {
        ExceptionCode = exceptionCode;
        SlaveId = slaveId;
        Function = function;
    }
}