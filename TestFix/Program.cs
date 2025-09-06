using System;
using ModbusLib.Models;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== 测试当前字节序修复效果 ===");
        Console.WriteLine($"系统字节序: {(BitConverter.IsLittleEndian ? "Little Endian" : "Big Endian")}");
        Console.WriteLine();

        TestSingleRegister();
        Console.WriteLine();
        TestMultipleRegisters();
        Console.WriteLine();
        TestSetLittleEndian();
    }

    static void TestSingleRegister()
    {
        Console.WriteLine("=== 单寄存器测试 ===");
        
        var buffer = new ushort[] { 0x1234 }.AsSpan();
        var result = buffer.GetLittleEndian<ushort>(0);
        
        Console.WriteLine($"输入: 0x{buffer[0]:X4}");
        Console.WriteLine($"GetLittleEndian<ushort>: {result} (0x{result:X4})");
        Console.WriteLine($"期望: 13330 (0x3412)");
        Console.WriteLine($"实际: {result} (0x{result:X4})");
        Console.WriteLine($"测试状态: {(result == 0x3412 ? "通过" : "失败")}");
    }

    static void TestMultipleRegisters()
    {
        Console.WriteLine("=== 多寄存器测试 ===");
        
        var buffer = new ushort[] { 0x1234, 0x5678 }.AsSpan();
        var result = buffer.GetLittleEndian<int>(0);
        
        Console.WriteLine($"输入: [0x{buffer[0]:X4}, 0x{buffer[1]:X4}]");
        Console.WriteLine($"GetLittleEndian<int>: {result} (0x{result:X8})");
        Console.WriteLine($"期望: 2018915346 (0x78563412)");
        Console.WriteLine($"实际: {result} (0x{result:X8})");
        Console.WriteLine($"测试状态: {(result == 0x78563412 ? "通过" : "失败")}");
    }

    static void TestSetLittleEndian()
    {
        Console.WriteLine("=== SetLittleEndian 测试 ===");
        
        // 测试 ushort
        var buffer1 = new ushort[1];
        buffer1.AsSpan().SetLittleEndian<ushort>(0, 0x1234);
        
        Console.WriteLine($"SetLittleEndian<ushort>(0x1234): 0x{buffer1[0]:X4}");
        Console.WriteLine($"期望: 0x3412, 实际: 0x{buffer1[0]:X4}");
        Console.WriteLine($"测试状态: {(buffer1[0] == 0x3412 ? "通过" : "失败")}");
        Console.WriteLine();
        
        // 测试 int
        var buffer2 = new ushort[2];
        buffer2.AsSpan().SetLittleEndian<int>(0, 0x12345678);
        
        Console.WriteLine($"SetLittleEndian<int>(0x12345678): [0x{buffer2[0]:X4}, 0x{buffer2[1]:X4}]");
        Console.WriteLine($"期望: [0x7856, 0x3412], 实际: [0x{buffer2[0]:X4}, 0x{buffer2[1]:X4}]");
        Console.WriteLine($"测试状态: {(buffer2[0] == 0x7856 && buffer2[1] == 0x3412 ? "通过" : "失败")}");
        Console.WriteLine();
        
        // 往返测试
        Console.WriteLine("=== 往返测试 ===");
        var originalValue = 0x12345678;
        var roundTripBuffer = new ushort[2];
        
        // 写入
        roundTripBuffer.AsSpan().SetLittleEndian<int>(0, originalValue);
        Console.WriteLine($"写入后寄存器: [0x{roundTripBuffer[0]:X4}, 0x{roundTripBuffer[1]:X4}]");
        
        // 读取
        var readValue = roundTripBuffer.AsSpan().GetLittleEndian<int>(0);
        Console.WriteLine($"读取值: 0x{readValue:X8}");
        Console.WriteLine($"原始值: 0x{originalValue:X8}");
        Console.WriteLine($"往返测试: {(originalValue == readValue ? "通过" : "失败")}");
        
        if (originalValue != readValue)
        {
            Console.WriteLine("往返测试失败！SetLittleEndian 和 GetLittleEndian 不一致");
        }
    }
}