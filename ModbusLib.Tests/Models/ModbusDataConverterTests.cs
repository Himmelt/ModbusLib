using ModbusLib.Enums;
using ModbusLib.Models;
using System.Reflection;
using Xunit.Abstractions;

namespace ModbusLib.Tests.Models;

public class ModbusDataConverterTests {

    public class GetRegisterCountTestCase : IXunitSerializable {
        public required Type Type { get; set; }
        public int Expected { get; set; }
        public string? Description { get; set; }

        public GetRegisterCountTestCase() { }

        public GetRegisterCountTestCase(Type type, int expected, string description) {
            Type = type;
            Expected = expected;
            Description = description;
        }

        public void Deserialize(IXunitSerializationInfo info) {
            Type = Type.GetType(info.GetValue<string>(nameof(Type)))!;
            Expected = info.GetValue<int>(nameof(Expected));
            Description = info.GetValue<string>(nameof(Description));
        }

        public void Serialize(IXunitSerializationInfo info) {
            info.AddValue(nameof(Type), Type?.AssemblyQualifiedName);
            info.AddValue(nameof(Expected), Expected);
            info.AddValue(nameof(Description), Description);
        }
    }

    [Theory]
    [MemberData(nameof(GetRegisterCountTestData))]
    public void GetRegisterCount(GetRegisterCountTestCase testCase) {
        var methodInfo = typeof(ModbusDataConverter)
            .GetMethod(nameof(ModbusDataConverter.GetRegisterCount), BindingFlags.Public | BindingFlags.Static)
            ?.MakeGenericMethod(testCase.Type);

        var result = methodInfo?.Invoke(null, null);
        Assert.Equal(testCase.Expected, (int)result!);
    }

    public static TheoryData<GetRegisterCountTestCase> GetRegisterCountTestData() {
        var data = new TheoryData<GetRegisterCountTestCase> {
            new() { Type = typeof(byte), Expected = 1, Description = "byte类型1字节=1个寄存器(向上取整)" },
            new() { Type = typeof(short), Expected = 1, Description = "short类型2字节=1个寄存器" },
            new() { Type = typeof(ushort), Expected = 1, Description = "ushort类型2字节=1个寄存器" },
            new() { Type = typeof(int), Expected = 2, Description = "int类型4字节=2个寄存器" },
            new() { Type = typeof(uint), Expected = 2, Description = "uint类型4字节=2个寄存器" },
            new() { Type = typeof(float), Expected = 2, Description = "float类型4字节=2个寄存器" },
            new() { Type = typeof(double), Expected = 4, Description = "double类型8字节=4个寄存器" },
            new() { Type = typeof(long), Expected = 4, Description = "long类型8字节=4个寄存器" },
            new() { Type = typeof(ulong), Expected = 4, Description = "ulong类型8字节=4个寄存器" }
        };
        return data;
    }

    public class GetTotalRegisterCountTestCase : IXunitSerializable {
        public required Type Type { get; set; }
        public int ElementCount { get; set; }
        public int Expected { get; set; }
        public string? Description { get; set; }

        public GetTotalRegisterCountTestCase() { }

        public GetTotalRegisterCountTestCase(Type type, int elementCount, int expected, string description) {
            Type = type;
            ElementCount = elementCount;
            Expected = expected;
            Description = description;
        }

        public void Deserialize(IXunitSerializationInfo info) {
            Type = Type.GetType(info.GetValue<string>(nameof(Type)))!;
            ElementCount = info.GetValue<int>(nameof(ElementCount));
            Expected = info.GetValue<int>(nameof(Expected));
            Description = info.GetValue<string>(nameof(Description));
        }

        public void Serialize(IXunitSerializationInfo info) {
            info.AddValue(nameof(Type), Type?.AssemblyQualifiedName);
            info.AddValue(nameof(ElementCount), ElementCount);
            info.AddValue(nameof(Expected), Expected);
            info.AddValue(nameof(Description), Description);
        }
    }

    [Theory]
    [MemberData(nameof(GetTotalRegisterCountTestData))]
    public void GetTotalRegisterCount(GetTotalRegisterCountTestCase testCase) {
        var methodInfo = typeof(ModbusDataConverter)
            .GetMethod(nameof(ModbusDataConverter.GetTotalRegisterCount), BindingFlags.Public | BindingFlags.Static)
            ?.MakeGenericMethod(testCase.Type);

        var result = methodInfo?.Invoke(null, [testCase.ElementCount]);
        Assert.Equal(testCase.Expected, (int)result!);
    }

    public static TheoryData<GetTotalRegisterCountTestCase> GetTotalRegisterCountTestData() {
        var data = new TheoryData<GetTotalRegisterCountTestCase> {
            new() { Type = typeof(byte), ElementCount = 7, Expected = 4, Description = "7个字节=4个寄存器(向上取整)" },
            new() { Type = typeof(short), ElementCount = 10, Expected = 10, Description = "10个short*每个1个寄存器=10个寄存器" },
            new() { Type = typeof(ushort), ElementCount = 5, Expected = 5, Description = "5个ushort*每个1个寄存器=5个寄存器" },
            new() { Type = typeof(int), ElementCount = 4, Expected = 8, Description = "4个int*每个2个寄存器=8个寄存器" },
            new() { Type = typeof(uint), ElementCount = 2, Expected = 4, Description = "2个uint*每个2个寄存器=4个寄存器" },
            new() { Type = typeof(float), ElementCount = 3, Expected = 6, Description = "3个float*每个2个寄存器=6个寄存器" },
            new() { Type = typeof(double), ElementCount = 1, Expected = 4, Description = "1个double*4个寄存器=4个寄存器" },
            new() { Type = typeof(long), ElementCount = 2, Expected = 8, Description = "2个long*每个4个寄存器=8个寄存器" },
            new() { Type = typeof(ulong), ElementCount = 2, Expected = 8, Description = "2个ulong*每个4个寄存器=8个寄存器" }
        };
        return data;
    }

    #region Convert<T>(T[] values) 方法测试

    [Fact]
    public void Convert_ShortArray_To_Bytes() {
        var values = new short[] { 0x1234, 0x5678 };
        var result11 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.HighFirst);
        var result12 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, result11);
        Assert.Equal(result11, result12);

        var result21 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.HighFirst);
        var result22 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0x34, 0x12, 0x78, 0x56 }, result21);
        Assert.Equal(result21, result22);
    }

    [Fact]
    public void Convert_IntArray_To_Bytes() {
        var values = new int[] { 0x12345678, 0x0abcdef0 };

        var result1 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.HighFirst);
        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x0a, 0xbc, 0xde, 0xf0 }, result1);
        var result2 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0x56, 0x78, 0x12, 0x34, 0xde, 0xf0, 0x0a, 0xbc }, result2);
        var result3 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.HighFirst);
        Assert.Equal(new byte[] { 0x34, 0x12, 0x78, 0x56, 0xbc, 0x0a, 0xf0, 0xde }, result3);
        var result4 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12, 0xf0, 0xde, 0xbc, 0x0a }, result4);
    }

    [Fact]
    public void Convert_FloatArray_To_Bytes() {
        var values = new float[] { 123.45f, 678.90f };

        var result1 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.HighFirst);
        Assert.Equal(new byte[] { 0x42, 0xF6, 0xE6, 0x66, 0x44, 0x29, 0xB9, 0x9A }, result1);
        var result2 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0xE6, 0x66, 0x42, 0xF6, 0xB9, 0x9A, 0x44, 0x29 }, result2);
        var result3 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.HighFirst);
        Assert.Equal(new byte[] { 0xF6, 0x42, 0x66, 0xE6, 0x29, 0x44, 0x9A, 0xB9 }, result3);
        var result4 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0x66, 0xE6, 0xF6, 0x42, 0x9A, 0xB9, 0x29, 0x44 }, result4);
    }

    [Fact]
    public void Convert_DoubleArray_To_Bytes() {
        var values = new double[] { 123.456789, 987.654321 };

        var result1 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.HighFirst);
        Assert.Equal(new byte[] { 0x40, 0x5E, 0xDD, 0x3C, 0x07, 0xEE, 0x0B, 0x0B, 0x40, 0x8E, 0xDD, 0x3C, 0x0C, 0xA6, 0x00, 0xB0 }, result1);
        var result2 = ModbusDataConverter.Convert(values, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0x0B, 0x0B, 0x07, 0xEE, 0xDD, 0x3C, 0x40, 0x5E, 0x00, 0xB0, 0x0C, 0xA6, 0xDD, 0x3C, 0x40, 0x8E }, result2);
        var result3 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.HighFirst);
        Assert.Equal(new byte[] { 0x5E, 0x40, 0x3C, 0xDD, 0xEE, 0x07, 0x0B, 0x0B, 0x8E, 0x40, 0x3C, 0xDD, 0xA6, 0x0C, 0xB0, 0x00 }, result3);
        var result4 = ModbusDataConverter.Convert(values, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(new byte[] { 0x0B, 0x0B, 0xEE, 0x07, 0x3C, 0xDD, 0x5E, 0x40, 0xB0, 0x00, 0xA6, 0x0C, 0x3C, 0xDD, 0x8E, 0x40 }, result4);
    }

    #endregion

    #region Convert<T>(byte[] bytes) 方法测试

    [Fact]
    public void Convert_Bytes_To_ShortArray() {
        // 使用正向转换测试中的数据
        var originalValues = new short[] { 0x1234, 0x5678 };

        // 测试大端字节序，高位优先
        var bytes1 = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var result11 = ModbusDataConverter.Convert<short>(bytes1, ByteOrder.BigEndian, WordOrder.HighFirst);
        var result12 = ModbusDataConverter.Convert<short>(bytes1, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result11);
        Assert.Equal(result11, result12);

        // 测试小端字节序，高位优先
        var bytes2 = new byte[] { 0x34, 0x12, 0x78, 0x56 };
        var result21 = ModbusDataConverter.Convert<short>(bytes2, ByteOrder.LittleEndian, WordOrder.HighFirst);
        var result22 = ModbusDataConverter.Convert<short>(bytes2, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result21);
        Assert.Equal(result21, result22);
    }

    [Fact]
    public void Convert_Bytes_To_IntArray() {
        // 使用正向转换测试中的数据
        var originalValues = new int[] { 0x12345678, 0x0abcdef0 };

        // 测试大端字节序，高位优先
        var bytes1 = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x0a, 0xbc, 0xde, 0xf0 };
        var result1 = ModbusDataConverter.Convert<int>(bytes1, ByteOrder.BigEndian, WordOrder.HighFirst);
        Assert.Equal(originalValues, result1);

        // 测试大端字节序，低位优先
        var bytes2 = new byte[] { 0x56, 0x78, 0x12, 0x34, 0xde, 0xf0, 0x0a, 0xbc };
        var result2 = ModbusDataConverter.Convert<int>(bytes2, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result2);

        // 测试小端字节序，高位优先
        var bytes3 = new byte[] { 0x34, 0x12, 0x78, 0x56, 0xbc, 0x0a, 0xf0, 0xde };
        var result3 = ModbusDataConverter.Convert<int>(bytes3, ByteOrder.LittleEndian, WordOrder.HighFirst);
        Assert.Equal(originalValues, result3);

        // 测试小端字节序，低位优先
        var bytes4 = new byte[] { 0x78, 0x56, 0x34, 0x12, 0xf0, 0xde, 0xbc, 0x0a };
        var result4 = ModbusDataConverter.Convert<int>(bytes4, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result4);
    }

    [Fact]
    public void Convert_Bytes_To_FloatArray() {
        // 使用正向转换测试中的数据
        var originalValues = new float[] { 123.45f, 678.90f };

        // 测试大端字节序，高位优先
        var bytes1 = new byte[] { 0x42, 0xF6, 0xE6, 0x66, 0x44, 0x29, 0xB9, 0x9A };
        var result1 = ModbusDataConverter.Convert<float>(bytes1, ByteOrder.BigEndian, WordOrder.HighFirst);
        Assert.Equal(originalValues, result1);

        // 测试大端字节序，低位优先
        var bytes2 = new byte[] { 0xE6, 0x66, 0x42, 0xF6, 0xB9, 0x9A, 0x44, 0x29 };
        var result2 = ModbusDataConverter.Convert<float>(bytes2, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result2);

        // 测试小端字节序，高位优先
        var bytes3 = new byte[] { 0xF6, 0x42, 0x66, 0xE6, 0x29, 0x44, 0x9A, 0xB9 };
        var result3 = ModbusDataConverter.Convert<float>(bytes3, ByteOrder.LittleEndian, WordOrder.HighFirst);
        Assert.Equal(originalValues, result3);

        // 测试小端字节序，低位优先
        var bytes4 = new byte[] { 0x66, 0xE6, 0xF6, 0x42, 0x9A, 0xB9, 0x29, 0x44 };
        var result4 = ModbusDataConverter.Convert<float>(bytes4, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result4);
    }

    [Fact]
    public void Convert_Bytes_To_DoubleArray() {
        // 使用正向转换测试中的数据
        var originalValues = new double[] { 123.456789, 987.654321 };

        // 测试大端字节序，高位优先
        var bytes1 = new byte[] { 0x40, 0x5E, 0xDD, 0x3C, 0x07, 0xEE, 0x0B, 0x0B, 0x40, 0x8E, 0xDD, 0x3C, 0x0C, 0xA6, 0x00, 0xB0 };
        var result1 = ModbusDataConverter.Convert<double>(bytes1, ByteOrder.BigEndian, WordOrder.HighFirst);
        Assert.Equal(originalValues, result1);

        // 测试大端字节序，低位优先
        var bytes2 = new byte[] { 0x0B, 0x0B, 0x07, 0xEE, 0xDD, 0x3C, 0x40, 0x5E, 0x00, 0xB0, 0x0C, 0xA6, 0xDD, 0x3C, 0x40, 0x8E };
        var result2 = ModbusDataConverter.Convert<double>(bytes2, ByteOrder.BigEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result2);

        // 测试小端字节序，高位优先
        var bytes3 = new byte[] { 0x5E, 0x40, 0x3C, 0xDD, 0xEE, 0x07, 0x0B, 0x0B, 0x8E, 0x40, 0x3C, 0xDD, 0xA6, 0x0C, 0xB0, 0x00 };
        var result3 = ModbusDataConverter.Convert<double>(bytes3, ByteOrder.LittleEndian, WordOrder.HighFirst);
        Assert.Equal(originalValues, result3);

        // 测试小端字节序，低位优先
        var bytes4 = new byte[] { 0x0B, 0x0B, 0xEE, 0x07, 0x3C, 0xDD, 0x5E, 0x40, 0xB0, 0x00, 0xA6, 0x0C, 0x3C, 0xDD, 0x8E, 0x40 };
        var result4 = ModbusDataConverter.Convert<double>(bytes4, ByteOrder.LittleEndian, WordOrder.LowFirst);
        Assert.Equal(originalValues, result4);
    }

    [Fact]
    public void Convert_Bytes_To_IntArray_ThrowsArgumentException() {
        var bytes = new byte[] { 0x12, 0x34, 0x56 };
        Assert.Throws<ArgumentException>(() => ModbusDataConverter.Convert<int>(bytes));
    }

    [Fact]
    public void Convert_EmptyBytes_To_IntArray_ThrowsArgumentException() {
        var bytes = Array.Empty<byte>();
        Assert.Throws<ArgumentException>(() => ModbusDataConverter.Convert<int>(bytes));
    }

    #endregion
}
