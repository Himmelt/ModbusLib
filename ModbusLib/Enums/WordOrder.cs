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

/// <summary>
/// WordOrder枚举的扩展方法
/// </summary>
public static class WordOrderExtensions {
    /// <summary>
    /// 判断字序是否低字在前
    /// </summary>
    /// <param name="wordOrder">字序</param>
    /// <returns>如果是低字在前则返回true，否则返回false</returns>
    public static bool IsLowFirst(this WordOrder wordOrder) {
        return wordOrder == WordOrder.LowFirst;
    }
}
