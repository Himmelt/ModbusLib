using ModbusLib.Enums;

namespace ModbusLib.Models;

/// <summary>
/// Modbus 请求基类
/// </summary>
public class ModbusRequest
{

    private readonly byte[]? _data;

    /// <summary>
    /// 从站地址
    /// </summary>
    public byte SlaveId { get; set; }

    /// <summary>
    /// 功能码
    /// </summary>
    public ModbusFunction Function { get; set; }

    /// <summary>
    /// 起始地址
    /// </summary>
    public ushort StartAddress { get; set; }

    /// <summary>
    /// 数据量
    /// </summary>
    public ushort Quantity { get; set; }

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

    public ModbusRequest()
    {
    }

    public ModbusRequest(byte slaveId, ModbusFunction function, ushort startAddress, ushort quantity, byte[]? data = null)
    {
        SlaveId = slaveId;
        Function = function;
        StartAddress = startAddress;
        Quantity = quantity;
        _data = data;
    }
}