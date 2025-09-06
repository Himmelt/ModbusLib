using System;
using ModbusLib.Models;

/// <summary>
/// 测试小端序修复效果
/// </summary>
public class TestLittleEndianFix
{
    public static void Main()
    {
        Console.WriteLine("=== 测试小端序修复效果 ===");
        Console.WriteLine();

        TestSingleRegister();
        Console.WriteLine();
        TestMultipleRegisters();
        Console.WriteLine();
        TestRoundTrip();
    }

    static void TestSingleRegister()
    {
        Console.WriteLine("=== 单寄存器测试 ===");
        
        // 测试 ushort
        var buffer = new ushort[] { 0x1234 }.AsSpan();
        var result = buffer.GetLittleEndian<ushort>(0);
        
        Console.WriteLine($"GetLittleEndian<ushort>(0x1234): {result} (0x{result:X4})");
        Console.WriteLine($"期望: 13330 (0x3412), 实际: {result} (0x{result:X4})");
        Console.WriteLine($"测试结果: {(result == 0x3412 ? "通过" : "失败")}");
        Console.WriteLine();

        // 测试 SetLittleEndian
        var setBuffer = new ushort[1];
        setBuffer.AsSpan().SetLittleEndian<ushort>(0, 0x1234);
        
        Console.WriteLine($"SetLittleEndian<ushort>(0x1234): buffer[0] = 0x{setBuffer[0]:X4}");
        Console.WriteLine($"期望: 0x3412, 实际: 0x{setBuffer[0]:X4}");
        Console.WriteLine($"测试结果: {(setBuffer[0] == 0x3412 ? "通过" : "失败")}");
    }

    static void TestMultipleRegisters()
    {
        Console.WriteLine("=== 多寄存器测试 ===");
        
        // 测试 int (2个寄存器)
        var buffer = new ushort[] { 0x1234, 0x5678 }.AsSpan();
        var result = buffer.GetLittleEndian<int>(0);
        
        Console.WriteLine($"GetLittleEndian<int>([0x1234, 0x5678]): {result} (0x{result:X8})");
        Console.WriteLine($"期望: 2018915346 (0x78563412), 实际: {result} (0x{result:X8})");
        Console.WriteLine($"测试结果: {(result == 0x78563412 ? "通过" : "失败")}");
        Console.WriteLine();

        // 测试 SetLittleEndian
        var setBuffer = new ushort[2];
        setBuffer.AsSpan().SetLittleEndian<int>(0, 0x12345678);
        
        Console.WriteLine($"SetLittleEndian<int>(0x12345678): [0x{setBuffer[0]:X4}, 0x{setBuffer[1]:X4}]");
        Console.WriteLine($"期望: [0x7856, 0x1234], 实际: [0x{setBuffer[0]:X4}, 0x{setBuffer[1]:X4}]");
        Console.WriteLine($"测试结果: {(setBuffer[0] == 0x7856 && setBuffer[1] == 0x1234 ? "通过" : "失败")}");
    }

    static void TestRoundTrip()
    {
        Console.WriteLine("=== 往返测试 ===");
        
        // 测试往返转换
        var originalValue = 0x12345678;
        var buffer = new ushort[2];
        
        // 写入
        buffer.AsSpan().SetLittleEndian<int>(0, originalValue);
        
        // 读取
        var readValue = buffer.AsSpan().GetLittleEndian<int>(0);
        
        Console.WriteLine($"原始值: 0x{originalValue:X8}");
        Console.WriteLine($"写入后寄存器: [0x{buffer[0]:X4}, 0x{buffer[1]:X4}]");
        Console.WriteLine($"读取值: 0x{readValue:X8}");
        Console.WriteLine($"往返测试: {(originalValue == readValue ? "通过" : "失败")}");
    }
}