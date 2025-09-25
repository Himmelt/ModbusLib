namespace ModbusLib.Enums;

/// <summary>
/// 字序枚举
/// </summary>
public enum WordOrder {
    /// <summary>
    /// 高字在前 (ABCD)
    /// </summary>
    HighFirst = 0,

    /// <summary>
    /// 低字在前 (CDAB)
    /// </summary>
    LowFirst = 1
}