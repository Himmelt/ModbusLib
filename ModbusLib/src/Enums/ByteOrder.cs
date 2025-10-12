namespace ModbusLib.Enums;

/// <summary>
/// 字节序枚举
/// </summary>
public enum ByteOrder {
    /// <summary>
    /// 大端序（高字节在前）
    /// </summary>
    BigEndian = 0,

    /// <summary>
    /// 小端序（低字节在前）
    /// </summary>
    LittleEndian = 1
}

/// <summary>
/// ByteOrder枚举的扩展方法
/// </summary>
public static class ByteOrderExtensions {
    /// <summary>
    /// 判断字节序是否为小端序
    /// </summary>
    /// <param name="byteOrder">字节序</param>
    /// <returns>如果是小端序则返回true，否则返回false</returns>
    public static bool IsLittleEndian(this ByteOrder byteOrder) {
        return byteOrder == ByteOrder.LittleEndian;
    }
}
