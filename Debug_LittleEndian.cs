using System;
using ModbusLib.Models;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== 小端序测试调试分析 ===");
        Console.WriteLine($"系统字节序: {(BitConverter.IsLittleEndian ? "Little Endian" : "Big Endian")}");
        Console.WriteLine();

        // 测试 1: GetLittleEndian_UShort_ReturnsCorrectValue
        Console.WriteLine("测试 1: GetLittleEndian_UShort");
        var buffer1 = new ushort[] { 0x1234 }.AsSpan();
        Console.WriteLine($"输入: buffer[0] = 0x{buffer1[0]:X4} ({buffer1[0]})");
        
        var result1 = buffer1.GetLittleEndian<ushort>(0);
        Console.WriteLine($"实际结果: {result1} (0x{result1:X4})");
        Console.WriteLine($"期望结果: 13330 (0x3412)");
        Console.WriteLine($"测试状态: {(result1 == 0x3412 ? "通过" : "失败")}");
        Console.WriteLine();

        // 分析字节交换过程
        Console.WriteLine("分析: 0x1234 在小端序中应该交换为 0x3412");
        Console.WriteLine("0x1234 = 高字节:0x12, 低字节:0x34");
        Console.WriteLine("小端序应该: 低字节在前 = 0x34, 高字节在后 = 0x12 => 0x3412");
        Console.WriteLine();

        // 测试 2: GetLittleEndian_Int_ReturnsCorrectValue
        Console.WriteLine("测试 2: GetLittleEndian_Int");
        var buffer2 = new ushort[] { 0x1234, 0x5678 }.AsSpan();
        Console.WriteLine($"输入: buffer[0] = 0x{buffer2[0]:X4}, buffer[1] = 0x{buffer2[1]:X4}");
        
        var result2 = buffer2.GetLittleEndian<int>(0);
        Console.WriteLine($"实际结果: {result2} (0x{result2:X8})");
        Console.WriteLine($"期望结果: 2018915346 (0x78563412)");
        Console.WriteLine($"测试状态: {(result2 == 0x78563412 ? "通过" : "失败")}");
        Console.WriteLine();

        Console.WriteLine("分析: 0x1234 5678 在小端序中的转换");
        Console.WriteLine("大端序布局: [0x1234][0x5678] = 字节序列: 12 34 56 78");
        Console.WriteLine("小端序期望: 78 56 34 12 = 0x78563412");
        Console.WriteLine();

        // 测试 3: SetLittleEndian_UShort_SetsCorrectValue
        Console.WriteLine("测试 3: SetLittleEndian_UShort");
        var buffer3 = new ushort[1];
        var span3 = buffer3.AsSpan();
        
        span3.SetLittleEndian<ushort>(0, 0x1234);
        Console.WriteLine($"设置值: 0x1234");
        Console.WriteLine($"实际结果: buffer[0] = 0x{buffer3[0]:X4} ({buffer3[0]})");
        Console.WriteLine($"期望结果: 0x3412 (13330)");
        Console.WriteLine($"测试状态: {(buffer3[0] == 0x3412 ? "通过" : "失败")}");
        Console.WriteLine();

        // 测试 4: SetLittleEndian_Int_SetsCorrectValue
        Console.WriteLine("测试 4: SetLittleEndian_Int");
        var buffer4 = new ushort[2];
        var span4 = buffer4.AsSpan();
        
        span4.SetLittleEndian<int>(0, 0x12345678);
        Console.WriteLine($"设置值: 0x12345678");
        Console.WriteLine($"实际结果: buffer[0] = 0x{buffer4[0]:X4}, buffer[1] = 0x{buffer4[1]:X4}");
        Console.WriteLine($"期望结果: buffer[0] = 0x7856 (30806), buffer[1] = 0x1234 (4660)");
        Console.WriteLine($"测试状态: {(buffer4[0] == 0x7856 && buffer4[1] == 0x1234 ? "通过" : "失败")}");
        Console.WriteLine();

        Console.WriteLine("分析: 0x12345678 在小端序中的寄存器排列");
        Console.WriteLine("字节序列: 12 34 56 78");
        Console.WriteLine("小端序期望: 78 56 34 12");
        Console.WriteLine("寄存器排列 (每个寄存器16位):");
        Console.WriteLine("- buffer[0] = 0x7856 (低16位，字节序78 56)");  
        Console.WriteLine("- buffer[1] = 0x1234 (高16位，字节序12 34)");
        Console.WriteLine();

        // 测试当前实现的实际行为
        Console.WriteLine("=== 当前实现分析 ===");
        AnalyzeCurrentImplementation();
    }

    static void AnalyzeCurrentImplementation()
    {
        Console.WriteLine("分析当前 GetLittleEndian 实现的问题:");
        
        // 模拟当前实现的逻辑
        var buffer = new ushort[] { 0x1234, 0x5678 }.AsSpan();
        
        // 当前实现可能的问题分析
        Console.WriteLine($"输入寄存器: [0x{buffer[0]:X4}, 0x{buffer[1]:X4}]");
        
        // 检查字节转换
        var reg1Bytes = new byte[] { (byte)(buffer[0] & 0xFF), (byte)(buffer[0] >> 8) };
        var reg2Bytes = new byte[] { (byte)(buffer[1] & 0xFF), (byte)(buffer[1] >> 8) };
        
        Console.WriteLine($"寄存器0字节: [0x{reg1Bytes[0]:X2}, 0x{reg1Bytes[1]:X2}] (低字节, 高字节)");
        Console.WriteLine($"寄存器1字节: [0x{reg2Bytes[0]:X2}, 0x{reg2Bytes[1]:X2}] (低字节, 高字节)");
        
        // 正确的小端序应该是什么
        Console.WriteLine();
        Console.WriteLine("正确的小端序转换应该是:");
        Console.WriteLine("1. 寄存器顺序反转: [0x5678, 0x1234]");
        Console.WriteLine("2. 每个寄存器内部字节顺序反转:");
        Console.WriteLine("   - 0x5678 -> 字节: 78 56");
        Console.WriteLine("   - 0x1234 -> 字节: 34 12");
        Console.WriteLine("3. 最终字节序列: 78 56 34 12 = 0x78563412");
    }
}