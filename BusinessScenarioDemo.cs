using System;
using ModbusLib.Models;

/// <summary>
/// ModbusSpanExtensions 业务场景演示
/// </summary>
public class BusinessScenarioDemo
{
    public static void Main()
    {
        Console.WriteLine("=== ModbusSpanExtensions 业务场景演示 ===");
        Console.WriteLine();

        // 模拟从 Modbus 设备读取的寄存器数据
        // 这些是从 PLC 或传感器设备中读取的原始寄存器值
        var registers = new ushort[] { 0x1234, 0x5678, 0x9ABC, 0xDEF0 };
        var span = registers.AsSpan();

        Console.WriteLine("=== 场景1: 温度传感器数据读取 ===");
        Console.WriteLine($"从设备读取的寄存器: [{string.Join(", ", Array.ConvertAll(registers, r => $"0x{r:X4}"))}]");
        Console.WriteLine();

        // 场景1：读取单字节数据（如状态位、小范围数值）
        DemonstrateByteReading(span);
        Console.WriteLine();

        // 场景2：读取整数数据（如温度、压力值）
        DemonstrateIntReading(span);
        Console.WriteLine();

        // 场景3：读取浮点数据（如精确测量值）
        DemonstrateFloatReading(span);
        Console.WriteLine();

        // 场景4：大小端序的实际意义
        DemonstrateEndiannessImportance();
    }

    static void DemonstrateByteReading(Span<ushort> span)
    {
        Console.WriteLine("=== 单字节读取示例 ===");
        Console.WriteLine("寄存器 0x1234 内部包含2个字节:");
        Console.WriteLine("  - 高字节: 0x12 (18)");
        Console.WriteLine("  - 低字节: 0x34 (52)");
        Console.WriteLine();

        // 当前GetLittleEndian<byte>的问题演示
        var currentResult = span.GetLittleEndian<byte>(0);
        Console.WriteLine($"当前 GetLittleEndian<byte>(0) 结果: {currentResult} (0x{currentResult:X2})");
        Console.WriteLine("问题: 这个结果取决于实现，但不够明确");
        Console.WriteLine();

        Console.WriteLine("应该的业务逻辑:");
        Console.WriteLine("- GetBigEndian<byte>(0) 应该返回高字节: 0x12 (18)");
        Console.WriteLine("- GetLittleEndian<byte>(0) 应该返回低字节: 0x34 (52)");
        Console.WriteLine();

        Console.WriteLine("实际业务应用:");
        Console.WriteLine("- 状态寄存器: 高字节=设备状态，低字节=错误代码");
        Console.WriteLine("- 配置寄存器: 高字节=模式设置，低字节=参数值");
    }

    static void DemonstrateIntReading(Span<ushort> span)
    {
        Console.WriteLine("=== 32位整数读取示例 ===");
        Console.WriteLine("业务场景: 读取温度传感器的精确温度值（32位有符号整数）");
        Console.WriteLine($"寄存器数据: [0x{span[0]:X4}, 0x{span[1]:X4}]");
        Console.WriteLine();

        var currentResult = span.GetLittleEndian<int>(0);
        Console.WriteLine($"当前 GetLittleEndian<int>(0) 结果: {currentResult} (0x{currentResult:X8})");
        Console.WriteLine("期望结果: 2018915346 (0x78563412)");
        Console.WriteLine();

        Console.WriteLine("业务意义:");
        Console.WriteLine("- 温度传感器返回: 0x12345678 表示 305,419,896 (℃ × 10000)");
        Console.WriteLine("- 小端序设备返回: 0x78563412 表示 2,018,915,346 (℃ × 10000)");
        Console.WriteLine("- 字节序错误会导致温度读数完全错误！");
    }

    static void DemonstrateFloatReading(Span<ushort> span)
    {
        Console.WriteLine("=== 32位浮点数读取示例 ===");
        Console.WriteLine("业务场景: 读取精密仪器的测量值（IEEE 754 浮点数）");
        Console.WriteLine();

        // 手动构造一个浮点数的字节表示
        float testValue = 3.14159f;
        var floatBytes = BitConverter.GetBytes(testValue);
        
        // 将字节转换为寄存器格式（大端序）
        var reg1 = (ushort)((floatBytes[3] << 8) | floatBytes[2]);
        var reg2 = (ushort)((floatBytes[1] << 8) | floatBytes[0]);
        var floatRegisters = new ushort[] { reg1, reg2 }.AsSpan();

        Console.WriteLine($"浮点数 {testValue} 的寄存器表示: [0x{reg1:X4}, 0x{reg2:X4}]");
        
        var readBack = floatRegisters.GetBigEndian<float>(0);
        Console.WriteLine($"通过 GetBigEndian<float> 读回: {readBack}");
        Console.WriteLine($"数据是否正确: {Math.Abs(testValue - readBack) < 0.0001f}");
        Console.WriteLine();

        Console.WriteLine("业务应用:");
        Console.WriteLine("- 压力传感器: 精确压力值 (bar)");
        Console.WriteLine("- 流量计: 瞬时流量 (m³/h)");
        Console.WriteLine("- 天平: 重量测量 (kg)");
        Console.WriteLine("- 字节序错误会导致测量数据完全不可用！");
    }

    static void DemonstrateEndiannessImportance()
    {
        Console.WriteLine("=== 大小端序的实际业务重要性 ===");
        Console.WriteLine();

        // 模拟不同厂商设备的数据格式
        var sameData = 0x12345678; // 同样的数据
        
        Console.WriteLine("同样的数据 0x12345678 在不同设备中的存储:");
        Console.WriteLine();

        Console.WriteLine("西门子 PLC (大端序):");
        Console.WriteLine("  寄存器0: 0x1234  寄存器1: 0x5678");
        Console.WriteLine("  读取: GetBigEndian<int>(0) → 305,419,896");
        Console.WriteLine();

        Console.WriteLine("施耐德 PLC (小端序):");
        Console.WriteLine("  寄存器0: 0x7856  寄存器1: 0x1234");
        Console.WriteLine("  读取: GetLittleEndian<int>(0) → 305,419,896");
        Console.WriteLine();

        Console.WriteLine("如果字节序处理错误:");
        Console.WriteLine("- 用大端序方法读小端序数据 → 错误的数值");
        Console.WriteLine("- 用小端序方法读大端序数据 → 错误的数值");
        Console.WriteLine("- 在工业控制中，这种错误可能导致严重事故！");
        Console.WriteLine();

        Console.WriteLine("ModbusSpanExtensions 解决的问题:");
        Console.WriteLine("1. 统一的接口处理不同厂商的字节序差异");
        Console.WriteLine("2. 类型安全的数据转换（byte, int, float, double等）");
        Console.WriteLine("3. 高性能的内存操作（使用Span<T>避免额外分配）");
        Console.WriteLine("4. 简化上层业务代码的复杂性");
    }
}