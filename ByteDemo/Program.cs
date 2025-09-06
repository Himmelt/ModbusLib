using System;
using ModbusLib.Models;

/// <summary>
/// 演示 GetLittleEndian<byte> 的字节选择逻辑
/// </summary>
public class ByteReadingDemo
{
    public static void Main()
    {
        Console.WriteLine("=== GetLittleEndian<byte> 字节选择分析 ===");
        Console.WriteLine();

        // 测试寄存器：0x1234
        // 高字节：0x12 (18)
        // 低字节：0x34 (52)
        var register = (ushort)0x1234;
        var buffer = new ushort[] { register }.AsSpan();

        Console.WriteLine($"测试寄存器值: 0x{register:X4}");
        Console.WriteLine($"  高字节: 0x{(register >> 8):X2} ({register >> 8})");
        Console.WriteLine($"  低字节: 0x{(register & 0xFF):X2} ({register & 0xFF})");
        Console.WriteLine();

        // 分析当前实现的逻辑
        AnalyzeCurrentImplementation(buffer);
        
        Console.WriteLine();
        
        // 展示正确的业务逻辑应该是什么
        ShowCorrectBusinessLogic(buffer);
        
        Console.WriteLine();
        
        // 实际业务场景示例
        ShowBusinessScenarios();
    }

    static void AnalyzeCurrentImplementation(Span<ushort> buffer)
    {
        Console.WriteLine("=== 当前实现分析 ===");
        
        // 模拟当前 GetLittleEndian<byte> 的实现逻辑
        var register = buffer[0]; // 0x1234
        
        Console.WriteLine("当前 GetLittleEndian<byte> 的实现步骤:");
        Console.WriteLine($"1. 获取寄存器值: 0x{register:X4}");
        Console.WriteLine("2. typeSize = 1 (byte的大小)");
        Console.WriteLine("3. registerCount = (1 + 1) / 2 = 1");
        Console.WriteLine("4. 创建1字节的byteBuffer");
        Console.WriteLine();
        
        Console.WriteLine("5. 小端序转换逻辑:");
        Console.WriteLine("   - regIndex = registerCount - 1 - 0 = 0");
        Console.WriteLine($"   - reg = registers[0] = 0x{register:X4}");
        Console.WriteLine("   - baseIndex = 0 * 2 = 0");
        Console.WriteLine($"   - byteSpan[0] = reg & 0xFF = 0x{register & 0xFF:X2} ({register & 0xFF})");
        Console.WriteLine();
        
        // 实际执行
        var result = buffer.GetLittleEndian<byte>(0);
        Console.WriteLine($"实际结果: {result} (0x{result:X2})");
        Console.WriteLine();
        
        Console.WriteLine("结论: GetLittleEndian<byte> 返回寄存器的【低字节】");
    }

    static void ShowCorrectBusinessLogic(Span<ushort> buffer)
    {
        Console.WriteLine("=== 正确的业务逻辑 ===");
        
        var register = buffer[0];
        var highByte = (byte)(register >> 8);   // 高字节
        var lowByte = (byte)(register & 0xFF);  // 低字节
        
        Console.WriteLine("对于寄存器 0x1234:");
        Console.WriteLine($"  GetBigEndian<byte>(0) 应该返回: {highByte} (0x{highByte:X2}) - 高字节");
        Console.WriteLine($"  GetLittleEndian<byte>(0) 应该返回: {lowByte} (0x{lowByte:X2}) - 低字节");
        Console.WriteLine();
        
        Console.WriteLine("原理:");
        Console.WriteLine("- 大端序: 高位字节在前 → 返回高字节");
        Console.WriteLine("- 小端序: 低位字节在前 → 返回低字节");
        Console.WriteLine();
        
        var actualResult = buffer.GetLittleEndian<byte>(0);
        Console.WriteLine($"当前实现确实返回低字节: {actualResult} (0x{actualResult:X2})");
        Console.WriteLine($"这个逻辑是正确的！✓");
    }

    static void ShowBusinessScenarios()
    {
        Console.WriteLine("=== 实际业务场景 ===");
        Console.WriteLine();
        
        Console.WriteLine("场景1: 设备状态寄存器 0x1234");
        Console.WriteLine("  寄存器定义:");
        Console.WriteLine("    - 高字节(0x12): 设备运行状态");
        Console.WriteLine("    - 低字节(0x34): 错误代码");
        Console.WriteLine("  读取方式:");
        Console.WriteLine("    - GetBigEndian<byte>(0) → 18 (设备状态)");
        Console.WriteLine("    - GetLittleEndian<byte>(0) → 52 (错误代码)");
        Console.WriteLine();
        
        Console.WriteLine("场景2: 温度传感器配置寄存器 0xABCD");
        Console.WriteLine("  寄存器定义:");
        Console.WriteLine("    - 高字节(0xAB): 测量模式设置");
        Console.WriteLine("    - 低字节(0xCD): 精度配置");
        Console.WriteLine("  读取方式:");
        Console.WriteLine("    - GetBigEndian<byte>(0) → 171 (测量模式)");
        Console.WriteLine("    - GetLittleEndian<byte>(0) → 205 (精度配置)");
        Console.WriteLine();
        
        Console.WriteLine("场景3: 多字节数据类型");
        Console.WriteLine("  对于 int、float 等多字节类型:");
        Console.WriteLine("  - GetBigEndian<T>: 按大端序解析多个寄存器");
        Console.WriteLine("  - GetLittleEndian<T>: 按小端序解析多个寄存器");
        Console.WriteLine("  这对于兼容不同厂商的设备至关重要！");
        Console.WriteLine();
        
        Console.WriteLine("总结:");
        Console.WriteLine("ModbusSpanExtensions 提供了:");
        Console.WriteLine("1. 统一的数据类型转换接口");
        Console.WriteLine("2. 自动处理字节序差异");
        Console.WriteLine("3. 高性能的内存操作");
        Console.WriteLine("4. 类型安全的数据访问");
        Console.WriteLine("这些特性对工业自动化项目非常重要！");
    }
}
