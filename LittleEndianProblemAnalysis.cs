using System;
using ModbusLib.Models;

/// <summary>
/// 小端序测试失败原因分析
/// </summary>
public class LittleEndianProblemAnalysis
{
    public static void Main()
    {
        Console.WriteLine("=== 小端序测试失败原因详细分析 ===");
        Console.WriteLine();

        // 分析问题1：GetLittleEndian<ushort> 的问题
        AnalyzeProblem1_GetLittleEndianUShort();
        Console.WriteLine();

        // 分析问题2：GetLittleEndian<int> 的问题
        AnalyzeProblem2_GetLittleEndianInt();
        Console.WriteLine();

        // 分析问题3：SetLittleEndian 的问题
        AnalyzeProblem3_SetLittleEndian();
        Console.WriteLine();

        // 总结所有问题
        SummarizeProblems();
    }

    static void AnalyzeProblem1_GetLittleEndianUShort()
    {
        Console.WriteLine("=== 问题1: GetLittleEndian<ushort> 失败分析 ===");
        Console.WriteLine("测试案例: GetLittleEndian_UShort_ReturnsCorrectValue");
        Console.WriteLine();

        var buffer = new ushort[] { 0x1234 }.AsSpan();
        Console.WriteLine($"输入: buffer[0] = 0x{buffer[0]:X4}");
        
        var actualResult = buffer.GetLittleEndian<ushort>(0);
        Console.WriteLine($"实际结果: {actualResult} (0x{actualResult:X4})");
        Console.WriteLine($"期望结果: 13330 (0x3412)");
        Console.WriteLine($"测试状态: {(actualResult == 0x3412 ? "通过" : "失败")}");
        Console.WriteLine();

        Console.WriteLine("问题分析:");
        Console.WriteLine("1. 输入寄存器: 0x1234");
        Console.WriteLine("2. typeSize = 2 (ushort), registerCount = 1");
        Console.WriteLine("3. 当前实现逻辑:");
        Console.WriteLine("   - regIndex = 1 - 1 - 0 = 0");
        Console.WriteLine("   - reg = registers[0] = 0x1234");
        Console.WriteLine("   - byteSpan[0] = reg & 0xFF = 0x34 (低字节)");
        Console.WriteLine("   - byteSpan[1] = reg >> 8 = 0x12 (高字节)");
        Console.WriteLine("   - 字节数组变成: [0x34, 0x12]");
        Console.WriteLine("4. MemoryMarshal.Read<ushort> 在小端序系统上:");
        Console.WriteLine("   - 读取 [0x34, 0x12] → 0x1234 (因为系统是小端序)");
        Console.WriteLine();
        Console.WriteLine("问题根因:");
        Console.WriteLine("- 当前实现没有正确处理单个寄存器的字节交换");
        Console.WriteLine("- 对于 ushort 类型，应该交换寄存器内部的字节顺序");
        Console.WriteLine("- 正确的结果应该是: 0x3412");
    }

    static void AnalyzeProblem2_GetLittleEndianInt()
    {
        Console.WriteLine("=== 问题2: GetLittleEndian<int> 失败分析 ===");
        Console.WriteLine("测试案例: GetLittleEndian_Int_ReturnsCorrectValue");
        Console.WriteLine();

        var buffer = new ushort[] { 0x1234, 0x5678 }.AsSpan();
        Console.WriteLine($"输入: buffer = [0x{buffer[0]:X4}, 0x{buffer[1]:X4}]");
        
        var actualResult = buffer.GetLittleEndian<int>(0);
        Console.WriteLine($"实际结果: {actualResult} (0x{actualResult:X8})");
        Console.WriteLine($"期望结果: 2018915346 (0x78563412)");
        Console.WriteLine($"测试状态: {(actualResult == 0x78563412 ? "通过" : "失败")}");
        Console.WriteLine();

        Console.WriteLine("当前实现的字节处理过程:");
        Console.WriteLine("1. registerCount = 2");
        Console.WriteLine("2. 寄存器处理顺序 (regIndex = registerCount - 1 - i):");
        Console.WriteLine("   - i=0: regIndex=1, reg=0x5678 → byteSpan[0]=0x78, byteSpan[1]=0x56");
        Console.WriteLine("   - i=1: regIndex=0, reg=0x1234 → byteSpan[2]=0x34, byteSpan[3]=0x12");
        Console.WriteLine("3. 结果字节数组: [0x78, 0x56, 0x34, 0x12]");
        Console.WriteLine("4. MemoryMarshal.Read<int> 在小端序系统:");
        Console.WriteLine("   - 读取 [0x78, 0x56, 0x34, 0x12] → 0x12345678");
        Console.WriteLine();

        Console.WriteLine("期望的正确处理:");
        Console.WriteLine("- 小端序应该产生字节序列: [0x78, 0x56, 0x34, 0x12]");
        Console.WriteLine("- 在小端序系统上读取应该得到: 0x78563412");
        Console.WriteLine();

        Console.WriteLine("问题根因:");
        Console.WriteLine("- 字节序列本身是正确的: [0x78, 0x56, 0x34, 0x12]");
        Console.WriteLine("- 但在小端序系统上，MemoryMarshal.Read<int> 会按小端序解释");
        Console.WriteLine("- 需要考虑系统字节序的影响！");
    }

    static void AnalyzeProblem3_SetLittleEndian()
    {
        Console.WriteLine("=== 问题3: SetLittleEndian 失败分析 ===");
        Console.WriteLine("测试案例: SetLittleEndian_UShort_SetsCorrectValue");
        Console.WriteLine();

        var buffer = new ushort[1];
        var span = buffer.AsSpan();
        
        span.SetLittleEndian<ushort>(0, 0x1234);
        Console.WriteLine($"设置值: 0x1234");
        Console.WriteLine($"实际结果: buffer[0] = 0x{buffer[0]:X4}");
        Console.WriteLine($"期望结果: 0x3412");
        Console.WriteLine($"测试状态: {(buffer[0] == 0x3412 ? "通过" : "失败")}");
        Console.WriteLine();

        Console.WriteLine("当前 SetLittleEndian 实现分析:");
        Console.WriteLine("1. MemoryMarshal.Write(byteSpan, 0x1234) 在小端序系统:");
        Console.WriteLine("   - 产生字节数组: [0x34, 0x12]");
        Console.WriteLine("2. 系统字节序调整 (BitConverter.IsLittleEndian = true):");
        Console.WriteLine("   - 条件 !BitConverter.IsLittleEndian = false，不执行 Reverse()");
        Console.WriteLine("3. 寄存器组装:");
        Console.WriteLine("   - register = byteSpan[0] | (byteSpan[1] << 8)");
        Console.WriteLine("   - register = 0x34 | (0x12 << 8) = 0x34 | 0x1200 = 0x1234");
        Console.WriteLine("4. 寄存器写入: registers[0] = 0x1234");
        Console.WriteLine();

        Console.WriteLine("问题根因:");
        Console.WriteLine("- SetLittleEndian 没有正确实现小端序存储逻辑");
        Console.WriteLine("- 应该将数据以小端序格式存储到寄存器中");
        Console.WriteLine("- 对于 0x1234，小端序存储应该是 0x3412");
    }

    static void SummarizeProblems()
    {
        Console.WriteLine("=== 问题总结 ===");
        Console.WriteLine();

        Console.WriteLine("核心问题:");
        Console.WriteLine("1. **字节序转换逻辑错误**: GetLittleEndian 和 SetLittleEndian 都没有正确实现小端序转换");
        Console.WriteLine();

        Console.WriteLine("2. **系统字节序处理不当**: 没有正确考虑系统字节序对 MemoryMarshal 操作的影响");
        Console.WriteLine();

        Console.WriteLine("3. **单寄存器字节交换缺失**: 对于单个寄存器，没有实现内部字节交换");
        Console.WriteLine();

        Console.WriteLine("修复策略:");
        Console.WriteLine("1. **重新设计小端序转换逻辑**");
        Console.WriteLine("   - 对于单个寄存器: 交换内部字节 (0x1234 → 0x3412)");
        Console.WriteLine("   - 对于多个寄存器: 反转寄存器顺序 + 交换每个寄存器内部字节");
        Console.WriteLine();

        Console.WriteLine("2. **正确处理系统字节序**");
        Console.WriteLine("   - 考虑 MemoryMarshal 在不同系统字节序下的行为差异");
        Console.WriteLine("   - 确保最终结果与期望的 Modbus 小端序格式一致");
        Console.WriteLine();

        Console.WriteLine("3. **统一 Get/Set 方法的逻辑**");
        Console.WriteLine("   - 确保 GetLittleEndian 和 SetLittleEndian 使用一致的转换算法");
        Console.WriteLine("   - 保证往返转换 (roundtrip) 的正确性");
        Console.WriteLine();

        Console.WriteLine("影响范围:");
        Console.WriteLine("- 所有使用小端序的 Modbus 设备通信将受影响");
        Console.WriteLine("- 数据读写错误可能导致设备控制异常");
        Console.WriteLine("- 在工业环境中，这种错误可能造成严重后果");
    }
}