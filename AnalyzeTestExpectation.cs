using System;

/// <summary>
/// 分析测试期望的小端序逻辑
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("=== 分析测试期望的小端序逻辑 ===");
        Console.WriteLine();

        // 分析测试期望
        Console.WriteLine("测试期望: SetLittleEndian<int>(0x12345678) → [0x7856, 0x1234]");
        Console.WriteLine();
        
        // 分析数据转换过程
        var value = 0x12345678;
        var bytes = BitConverter.GetBytes(value);
        
        Console.WriteLine($"0x{value:X8} 的字节表示 (系统字节序): [{string.Join(", ", Array.ConvertAll(bytes, b => $"0x{b:X2}"))}]");
        
        if (BitConverter.IsLittleEndian)
        {
            Console.WriteLine("系统是小端序，所以字节顺序是: [0x78, 0x56, 0x34, 0x12]");
        }
        
        Console.WriteLine();
        Console.WriteLine("分析期望的寄存器排列:");
        Console.WriteLine("- buffer[0] = 0x7856");
        Console.WriteLine("- buffer[1] = 0x1234");
        Console.WriteLine();
        
        Console.WriteLine("这意味着:");
        Console.WriteLine("- 第0个寄存器: 从字节[0x78, 0x56] 组成 0x7856 (小端序)");
        Console.WriteLine("- 第1个寄存器: 从字节[0x34, 0x12] 组成 0x1234 (大端序)");
        Console.WriteLine();
        
        Console.WriteLine("这是一种混合策略:");
        Console.WriteLine("1. 按照小端序排列寄存器 (低位寄存器在前)");
        Console.WriteLine("2. 第一个寄存器内部使用小端序字节排列");
        Console.WriteLine("3. 第二个寄存器内部使用大端序字节排列");
        Console.WriteLine();
        
        // 验证往返测试应该如何工作
        Console.WriteLine("=== 验证往返测试逻辑 ===");
        Console.WriteLine("如果寄存器是 [0x7856, 0x1234]");
        Console.WriteLine("GetLittleEndian 应该读取为:");
        
        // 模拟 GetLittleEndian 的逻辑
        // registerCount = 2
        // for i in [0, 1]:
        //   regIndex = 2 - 1 - i = [1, 0]  -> 寄存器顺序反转
        //   字节提取: reg & 0xFF, reg >> 8
        
        var reg0 = (ushort)0x7856;
        var reg1 = (ushort)0x1234;
        
        Console.WriteLine($"寄存器[1] = 0x{reg1:X4} → 字节: 0x{reg1 & 0xFF:X2}, 0x{reg1 >> 8:X2}");
        Console.WriteLine($"寄存器[0] = 0x{reg0:X4} → 字节: 0x{reg0 & 0xFF:X2}, 0x{reg0 >> 8:X2}");
        
        // 按照当前GetLittleEndian的逻辑
        byte[] resultBytes = new byte[4];
        resultBytes[0] = (byte)(reg1 & 0xFF);     // 从reg1取低字节 = 0x34
        resultBytes[1] = (byte)(reg1 >> 8);       // 从reg1取高字节 = 0x12
        resultBytes[2] = (byte)(reg0 & 0xFF);     // 从reg0取低字节 = 0x56
        resultBytes[3] = (byte)(reg0 >> 8);       // 从reg0取高字节 = 0x78
        
        Console.WriteLine($"提取的字节序列: [{string.Join(", ", Array.ConvertAll(resultBytes, b => $"0x{b:X2}"))}]");
        
        // 在小端序系统上，需要反转
        Array.Reverse(resultBytes);
        Console.WriteLine($"反转后字节序列: [{string.Join(", ", Array.ConvertAll(resultBytes, b => $"0x{b:X2}"))}]");
        
        var finalValue = BitConverter.ToInt32(resultBytes, 0);
        Console.WriteLine($"最终读取值: 0x{finalValue:X8}");
        Console.WriteLine($"往返测试: {(finalValue == value ? "成功" : "失败")}");
    }
}