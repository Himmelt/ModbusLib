using ModbusLib.Enums;

namespace ModbusLib.Models;

/// <summary>
/// Modbus 响应基类
/// </summary>
public class ModbusResponse {
    private readonly byte[]? _data;
    private readonly byte[]? _rawData;

    /// <summary>
    /// 设备地址
    /// </summary>
    public byte UnitId { get; set; }

    /// <summary>
    /// 功能码
    /// </summary>
    public ModbusFunction Function { get; set; }

    /// <summary>
    /// 数据内容
    /// </summary>
    public ReadOnlySpan<byte> Data {
        get => _data;
    }

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
    public ReadOnlySpan<byte> RawData {
        get => _rawData;
    }

    public ModbusResponse() {
    }

    public ModbusResponse(byte unitId, ModbusFunction function, byte[]? data = null, byte[]? rawData = null) {
        UnitId = unitId;
        Function = function;
        _data = data;
        _rawData = rawData;
    }

    /// <summary>
    /// 创建错误响应
    /// </summary>
    public static ModbusResponse CreateError(byte unitId, ModbusFunction function, ModbusExceptionCode exceptionCode) {
        return new ModbusResponse {
            UnitId = unitId,
            Function = function,
            IsError = true,
            ExceptionCode = exceptionCode
        };
    }
}