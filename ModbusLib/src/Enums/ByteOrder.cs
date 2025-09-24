namespace ModbusLib.Enums;

/// <summary>
/// 字节序枚举
/// </summary>
public enum ByteOrder : byte
{
    /// <summary>
    /// 大端序（高字节在前）
    /// </summary>
    BigEndian = 0,
    
    /// <summary>
    /// 小端序（低字节在前）
    /// </summary>
    LittleEndian = 1
}