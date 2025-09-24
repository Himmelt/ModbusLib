using System.Diagnostics.CodeAnalysis;

namespace ModbusLib.Enums;

/// <summary>
/// Modbus 功能码枚举
/// </summary>
[SuppressMessage("Design", "CA1008:枚举应具有零值")]
[SuppressMessage("CodeQuality", "IDE0079:请删除不必要的忽略")]
public enum ModbusFunction 
{
    /// <summary>
    /// 读取线圈状态 (0x01)
    /// </summary>
    ReadCoils = 0x01,

    /// <summary>
    /// 读取离散输入状态 (0x02)
    /// </summary>
    ReadDiscreteInputs = 0x02,

    /// <summary>
    /// 读取保持寄存器 (0x03)
    /// </summary>
    ReadHoldingRegisters = 0x03,

    /// <summary>
    /// 读取输入寄存器 (0x04)
    /// </summary>
    ReadInputRegisters = 0x04,

    /// <summary>
    /// 写单个线圈 (0x05)
    /// </summary>
    WriteSingleCoil = 0x05,

    /// <summary>
    /// 写单个寄存器 (0x06)
    /// </summary>
    WriteSingleRegister = 0x06,

    /// <summary>
    /// 写多个线圈 (0x0F)
    /// </summary>
    WriteMultipleCoils = 0x0F,

    /// <summary>
    /// 写多个寄存器 (0x10)
    /// </summary>
    WriteMultipleRegisters = 0x10,

    /// <summary>
    /// 读写多个寄存器 (0x17)
    /// </summary>
    ReadWriteMultipleRegisters = 0x17
}