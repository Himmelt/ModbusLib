using ModbusLib.Enums;

namespace ModbusLib.Models;

/// <summary>
/// Modbus 响应基类
/// </summary>
public class ModbusResponse
{
    /// <summary>
    /// 从站地址
    /// </summary>
    public byte SlaveId { get; set; }

    /// <summary>
    /// 功能码
    /// </summary>
    public ModbusFunction Function { get; set; }

    /// <summary>
    /// 数据内容
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否错误响应
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// 异常码
    /// </summary>
    public ModbusExceptionCode? ExceptionCode { get; set; }

    /// <summary>
    /// 响应的原始字节数据
    /// </summary>
    public byte[]? RawData { get; set; }

    public ModbusResponse()
    {
    }

    public ModbusResponse(byte slaveId, ModbusFunction function, byte[]? data = null)
    {
        SlaveId = slaveId;
        Function = function;
        Data = data;
    }

    /// <summary>
    /// 创建错误响应
    /// </summary>
    public static ModbusResponse CreateError(byte slaveId, ModbusFunction function, ModbusExceptionCode exceptionCode)
    {
        return new ModbusResponse
        {
            SlaveId = slaveId,
            Function = function,
            IsError = true,
            ExceptionCode = exceptionCode
        };
    }
}