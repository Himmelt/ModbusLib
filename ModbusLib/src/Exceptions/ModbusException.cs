using ModbusLib.Enums;

namespace ModbusLib.Exceptions;

/// <summary>
/// Modbus 异常基类
/// </summary>
public class ModbusException : Exception {
    /// <summary>
    /// 异常码
    /// </summary>
    public ModbusExceptionCode ExceptionCode { get; }

    /// <summary>
    /// 设备地址
    /// </summary>
    public byte UnitId { get; }

    /// <summary>
    /// 功能码
    /// </summary>
    public ModbusFunction Function { get; }

    public ModbusException() { }

    public ModbusException(string message) : base(message) { }

    public ModbusException(string message, Exception innerEx) : base(message, innerEx) { }

    public ModbusException(ModbusExceptionCode exCode, byte unitId, ModbusFunction func, string message) : base(message) {
        ExceptionCode = exCode;
        UnitId = unitId;
        Function = func;
    }

    public ModbusException(ModbusExceptionCode exCode, byte unitId, ModbusFunction func, string message, Exception innerEx) : base(message, innerEx) {
        ExceptionCode = exCode;
        UnitId = unitId;
        Function = func;
    }

    public ModbusException(ModbusExceptionCode exCode, byte unitId, ModbusFunction func)
        : base($"Modbus异常: 设备地址{unitId}, 功能码{(byte)func:X2}, 异常码{(byte)exCode:X2}: {exCode.GetDescription()}") {
        ExceptionCode = exCode;
        UnitId = unitId;
        Function = func;
    }
}
