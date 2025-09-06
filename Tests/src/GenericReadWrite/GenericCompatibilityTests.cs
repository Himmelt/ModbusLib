using Xunit;
using ModbusLib.Models;
using ModbusLib.Enums;
using ModbusLib.Tests;
using System.Runtime.InteropServices;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// 复杂数据类型兼容性测试
    /// </summary>
    public class GenericCompatibilityTests : IDisposable
    {
        private readonly TestModbusClient _client;
        
        public GenericCompatibilityTests()
        {
            _client = new TestModbusClient();
        }
        
        #region 基础类型测试
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public async Task GenericReadWrite_AllBasicTypes_Success(ModbusEndianness endianness)
        {
            // Test byte
            var byteValues = new byte[] { 0x12, 0x34, 0x56 };
            await TestRoundTrip(byteValues, endianness, (ushort)100);
            
            // Test sbyte
            var sbyteValues = new sbyte[] { -1, 0, 127 };
            await TestRoundTrip(sbyteValues, endianness, (ushort)110);
            
            // Test ushort
            var ushortValues = new ushort[] { 0x1234, 0x5678 };
            await TestRoundTrip(ushortValues, endianness, (ushort)120);
            
            // Test short
            var shortValues = new short[] { -1000, 0, 32000 };
            await TestRoundTrip(shortValues, endianness, (ushort)130);
            
            // Test uint
            var uintValues = new uint[] { 0x12345678u, 0x87654321u };
            await TestRoundTrip(uintValues, endianness, (ushort)140);
            
            // Test int
            var intValues = new int[] { -123456789, 0, 987654321 };
            await TestRoundTrip(intValues, endianness, (ushort)150);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public async Task GenericReadWrite_LongTypes_Success(ModbusEndianness endianness)
        {
            // Test ulong
            var ulongValues = new ulong[] { 0x123456789ABCDEFul, ulong.MaxValue };
            await TestRoundTrip(ulongValues, endianness, (ushort)200);
            
            // Test long
            var longValues = new long[] { long.MinValue, 0, long.MaxValue };
            await TestRoundTrip(longValues, endianness, (ushort)210);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public async Task GenericReadWrite_FloatingPointTypes_Success(ModbusEndianness endianness)
        {
            // Test float
            var floatValues = new float[] { 3.14159f, -2.71828f, float.MaxValue, float.MinValue };
            await TestFloatRoundTrip(floatValues, endianness, (ushort)300);
            
            // Test double
            var doubleValues = new double[] { Math.PI, Math.E, double.MaxValue, double.MinValue };
            await TestDoubleRoundTrip(doubleValues, endianness, (ushort)310);
        }
        
        #endregion
        
        #region 结构体类型测试
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SimpleStruct
        {
            public byte ByteValue;
            public ushort UShortValue;
            public int IntValue;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ComplexStruct
        {
            public float FloatValue;
            public double DoubleValue;
            public long LongValue;
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public async Task GenericReadWrite_SimpleStruct_Success(ModbusEndianness endianness)
        {
            // Arrange
            var originalValues = new SimpleStruct[]
            {
                new() { ByteValue = 0x12, UShortValue = 0x3456, IntValue = 0x789ABCDE },
                new() { ByteValue = 0xAB, UShortValue = 0xCDEF, IntValue = -123456789 }
            };
            
            await TestRoundTrip(originalValues, endianness, (ushort)400);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public async Task GenericReadWrite_ComplexStruct_Success(ModbusEndianness endianness)
        {
            // Arrange
            var originalValues = new ComplexStruct[]
            {
                new() { FloatValue = 3.14159f, DoubleValue = Math.PI, LongValue = long.MaxValue },
                new() { FloatValue = -2.71828f, DoubleValue = Math.E, LongValue = long.MinValue }
            };
            
            await TestComplexStructRoundTrip(originalValues, endianness, (ushort)500);
        }
        
        #endregion
        
        #region 边界值测试
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public async Task GenericReadWrite_MinMaxValues_Success(ModbusEndianness endianness)
        {
            // Test byte min/max
            var byteMinMax = new byte[] { byte.MinValue, byte.MaxValue };
            await TestRoundTrip(byteMinMax, endianness, (ushort)600);
            
            // Test short min/max
            var shortMinMax = new short[] { short.MinValue, short.MaxValue };
            await TestRoundTrip(shortMinMax, endianness, (ushort)610);
            
            // Test int min/max
            var intMinMax = new int[] { int.MinValue, int.MaxValue };
            await TestRoundTrip(intMinMax, endianness, (ushort)620);
            
            // Test uint min/max
            var uintMinMax = new uint[] { uint.MinValue, uint.MaxValue };
            await TestRoundTrip(uintMinMax, endianness, (ushort)630);
        }
        
        [Fact]
        public async Task GenericReadWrite_SpecialFloatValues_Success()
        {
            // Arrange - 测试特殊浮点值
            var specialFloats = new float[] 
            { 
                float.NaN, 
                float.PositiveInfinity, 
                float.NegativeInfinity, 
                0.0f, 
                -0.0f,
                float.Epsilon,
                1.0f / 3.0f  // 无法精确表示的值
            };
            
            await TestSpecialFloatRoundTrip(specialFloats, ModbusEndianness.BigEndian, (ushort)700);
        }
        
        [Fact]
        public async Task GenericReadWrite_SpecialDoubleValues_Success()
        {
            // Arrange - 测试特殊双精度值
            var specialDoubles = new double[] 
            { 
                double.NaN, 
                double.PositiveInfinity, 
                double.NegativeInfinity, 
                0.0, 
                -0.0,
                double.Epsilon,
                1.0 / 3.0  // 无法精确表示的值
            };
            
            await TestSpecialDoubleRoundTrip(specialDoubles, ModbusEndianness.BigEndian, (ushort)800);
        }
        
        #endregion
        
        #region 大数组测试
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        public async Task GenericReadWrite_LargeByteArray_Success(ModbusEndianness endianness)
        {
            // Arrange - 创建大型字节数组（接近限制但不超过）
            const int maxBytes = 246; // 恰好需要123个寄存器
            var largeByteArray = new byte[maxBytes];
            for (int i = 0; i < maxBytes; i++)
            {
                largeByteArray[i] = (byte)(i % 256);
            }
            
            await TestRoundTrip(largeByteArray, endianness, (ushort)900);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        public async Task GenericReadWrite_LargeIntArray_Success(ModbusEndianness endianness)
        {
            // Arrange - 创建大型int数组
            const int maxInts = 61; // 61个int需要122个寄存器
            var largeIntArray = new int[maxInts];
            for (int i = 0; i < maxInts; i++)
            {
                largeIntArray[i] = i * 123456;
            }
            
            await TestRoundTrip(largeIntArray, endianness, (ushort)1000);
        }
        
        #endregion
        
        #region 辅助方法
        
        private async Task TestRoundTrip<T>(T[] originalValues, ModbusEndianness endianness, ushort baseAddress) 
            where T : unmanaged
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = baseAddress;
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<T>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<T>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<T>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            Assert.Equal(originalValues, readValues);
        }
        
        private async Task TestFloatRoundTrip(float[] originalValues, ModbusEndianness endianness, ushort baseAddress)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = baseAddress;
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<float>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<float>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<float>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i], readValues[i], 5); // 5位精度
            }
        }
        
        private async Task TestDoubleRoundTrip(double[] originalValues, ModbusEndianness endianness, ushort baseAddress)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = baseAddress;
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<double>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<double>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<double>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i], readValues[i], 10); // 10位精度
            }
        }
        
        private async Task TestComplexStructRoundTrip(ComplexStruct[] originalValues, ModbusEndianness endianness, ushort baseAddress)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = baseAddress;
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<ComplexStruct>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<ComplexStruct>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<ComplexStruct>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i].FloatValue, readValues[i].FloatValue, 5);
                Assert.Equal(originalValues[i].DoubleValue, readValues[i].DoubleValue, 10);
                Assert.Equal(originalValues[i].LongValue, readValues[i].LongValue);
            }
        }
        
        private async Task TestSpecialFloatRoundTrip(float[] originalValues, ModbusEndianness endianness, ushort baseAddress)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = baseAddress;
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<float>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<float>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<float>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                if (float.IsNaN(originalValues[i]))
                {
                    Assert.True(float.IsNaN(readValues[i]));
                }
                else
                {
                    Assert.Equal(originalValues[i], readValues[i]);
                }
            }
        }
        
        private async Task TestSpecialDoubleRoundTrip(double[] originalValues, ModbusEndianness endianness, ushort baseAddress)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = baseAddress;
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<double>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<double>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<double>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                if (double.IsNaN(originalValues[i]))
                {
                    Assert.True(double.IsNaN(readValues[i]));
                }
                else
                {
                    Assert.Equal(originalValues[i], readValues[i]);
                }
            }
        }
        
        /// <summary>
        /// 将泛型数组转换为ushort寄存器数组
        /// </summary>
        private static ushort[] ConvertToRegisters<T>(T[] values, ModbusEndianness endianness) where T : unmanaged
        {
            var bytes = ModbusDataConverter.ToBytes(values, endianness);
            return TestHelper.BytesToUshortArray(bytes);
        }
        
        #endregion
        
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}