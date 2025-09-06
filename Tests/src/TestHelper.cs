using Xunit;

namespace ModbusLib.Tests;

/// <summary>
/// 基础测试帮助类
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// 测试用从站ID
    /// </summary>
    public const byte TestSlaveId = 1;
    
    /// <summary>
    /// 测试用起始地址
    /// </summary>
    public const ushort TestStartAddress = 0;
    
    /// <summary>
    /// 默认超时时间
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// 创建测试用的bool数组
    /// </summary>
    /// <param name="length">数组长度</param>
    /// <param name="fillValue">填充值</param>
    /// <returns>bool数组</returns>
    public static bool[] CreateTestBoolArray(int length, bool fillValue = true)
    {
        var result = new bool[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = fillValue;
        }
        return result;
    }
    
    /// <summary>
    /// 创建测试用的ushort数组
    /// </summary>
    /// <param name="length">数组长度</param>
    /// <param name="startValue">起始值</param>
    /// <returns>ushort数组</returns>
    public static ushort[] CreateTestUshortArray(int length, ushort startValue = 100)
    {
        var result = new ushort[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = (ushort)(startValue + i);
        }
        return result;
    }
    
    /// <summary>
    /// 将字节数组转换为ushort数组（用于测试）
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <returns>ushort数组</returns>
    public static ushort[] BytesToUshortArray(byte[] bytes)
    {
        var result = new ushort[(bytes.Length + 1) / 2];
        for (int i = 0; i < result.Length; i++)
        {
            var baseIndex = i * 2;
            ushort value = 0;
            if (baseIndex < bytes.Length)
                value = (ushort)(bytes[baseIndex] << 8);
            if (baseIndex + 1 < bytes.Length)
                value |= bytes[baseIndex + 1];
            result[i] = value;
        }
        return result;
    }
}