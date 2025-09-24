using System.Diagnostics.CodeAnalysis;

namespace ModbusLib.Enums;

/// <summary>
/// Modbus 异常码枚举
/// </summary>
[SuppressMessage("Design", "CA1027:用 FlagsAttribute 标记枚举")]
[SuppressMessage("CodeQuality", "IDE0079:请删除不必要的忽略")]
public enum ModbusExceptionCode
{
    /// <summary>
    /// 无异常
    /// </summary>
    None = 0x00,

    /// <summary>
    /// 非法功能码
    /// </summary>
    IllegalFunction = 0x01,

    /// <summary>
    /// 非法数据地址
    /// </summary>
    IllegalDataAddress = 0x02,

    /// <summary>
    /// 非法数据值
    /// </summary>
    IllegalDataValue = 0x03,

    /// <summary>
    /// 从站设备故障
    /// </summary>
    SlaveDeviceFailure = 0x04,

    /// <summary>
    /// 确认
    /// </summary>
    Acknowledge = 0x05,

    /// <summary>
    /// 从站设备忙
    /// </summary>
    SlaveDeviceBusy = 0x06,

    /// <summary>
    /// 内存校验错误
    /// </summary>
    MemoryParityError = 0x08,

    /// <summary>
    /// 网关路径不可用
    /// </summary>
    GatewayPathUnavailable = 0x0A,

    /// <summary>
    /// 网关目标设备无响应
    /// </summary>
    GatewayTargetDeviceFailedToRespond = 0x0B
}