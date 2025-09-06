using System;
using ModbusLib.Models;
using ModbusLib.Enums;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== 小端序测试和数据转换测试详细分析 ===");
        Console.WriteLine($"系统字节序: {(BitConverter.IsLittleEndian ? "Little Endian" : "Big Endian")}");
        Console.WriteLine();

        TestModbusSpanExtensionsLittleEndian();
        TestModbusDataConverterBigEndian();
        AnalyzeProblemSummary();
    }

    static void TestModbusSpanExtensionsLittleEndian()
    {
        Console.WriteLine("=== ModbusSpanExtensions 小端序测试 ===");
        
        // 测试 GetLittleEndian_UShort
        var buffer1 = new ushort[] { 0x1234 }.AsSpan();
        var result1 = buffer1.GetLittleEndian<ushort>(0);
        Console.WriteLine($"GetLittleEndian_UShort: 输入 0x{buffer1[0]:X4}, 实际 {result1} (0x{result1:X4}), 期望 13330 (0x3412), 状态: {(result1 == 0x3412 ? "通过" : "失败")}");
        
        // 测试 GetLittleEndian_Int
        var buffer2 = new ushort[] { 0x1234, 0x5678 }.AsSpan();
        var result2 = buffer2.GetLittleEndian<int>(0);
        Console.WriteLine($"GetLittleEndian_Int: 输入 [0x{buffer2[0]:X4},0x{buffer2[1]:X4}], 实际 {result2} (0x{result2:X8}), 期望 2018915346 (0x78563412), 状态: {(result2 == 0x78563412 ? "通过" : "失败")}");
        
        // 测试 SetLittleEndian_UShort
        var buffer3 = new ushort[1];
        buffer3.AsSpan().SetLittleEndian<ushort>(0, 0x1234);
        Console.WriteLine($"SetLittleEndian_UShort: 设置 0x1234, 实际 buffer[0]=0x{buffer3[0]:X4}, 期望 0x3412, 状态: {(buffer3[0] == 0x3412 ? "通过" : "失败")}");
        
        // 测试 SetLittleEndian_Int
        var buffer4 = new ushort[2];
        buffer4.AsSpan().SetLittleEndian<int>(0, 0x12345678);
        Console.WriteLine($"SetLittleEndian_Int: 设置 0x12345678, 实际 [0x{buffer4[0]:X4},0x{buffer4[1]:X4}], 期望 [0x7856,0x1234], 状态: {(buffer4[0] == 0x7856 && buffer4[1] == 0x1234 ? "通过" : "失败")}");
        Console.WriteLine();
    }

    static void TestModbusDataConverterBigEndian()
    {
        Console.WriteLine("=== ModbusDataConverter 大端序测试 ===");
        
        var values = new int[] { unchecked((int)0x12345678) };
        var result = ModbusDataConverter.ToBytes(values, ModbusEndianness.BigEndian);
        
        Console.WriteLine($"ToBytes_IntArray_BigEndian: 输入 0x{values[0]:X8}");
        Console.WriteLine($"  实际结果: [{string.Join(", ", Array.ConvertAll(result, b => $"0x{b:X2}"))}]");
        
        if (BitConverter.IsLittleEndian)
        {
            var expected = new byte[] { 0x78, 0x56, 0x34, 0x12 };
            Console.WriteLine($"  期望结果: [{string.Join(", ", Array.ConvertAll(expected, b => $"0x{b:X2}"))}]");
            
            bool passed = result.Length == 4 && result[0] == 0x78 && result[1] == 0x56 && result[2] == 0x34 && result[3] == 0x12;
            Console.WriteLine($"  测试状态: {(passed ? "通过" : "失败")}");
            
            if (!passed)
            {
                int failIndex = -1;
                for (int i = 0; i < Math.Min(result.Length, expected.Length); i++)
                {
                    if (result[i] != expected[i]) { failIndex = i; break; }
                }
                Console.WriteLine($"  失败详情: 在第{failIndex}个字节上差异");
            }
        }
        Console.WriteLine();
    }

    static void AnalyzeProblemSummary()
    {
        Console.WriteLine("=== 问题总结 ===");
        Console.WriteLine("1. ModbusSpanExtensions 小端序问题: GetLittleEndian/SetLittleEndian 没有正确实现字节交换");
        Console.WriteLine("2. ModbusDataConverter 大端序问题: ToBytes 在小端序系统上处理大端序转换不正确");
        Console.WriteLine("3. 核心问题: 字节序转换逻辑需要重新实现以符合 Modbus 协议标准");
        Console.WriteLine("4. 影响: 小端序数据读写和大端序数据转换将无法正确工作");
    }
}
