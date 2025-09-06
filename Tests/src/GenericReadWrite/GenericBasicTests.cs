using Xunit;
using ModbusLib.Models;
using ModbusLib.Enums;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// 简化的泛型功能基础测试
    /// </summary>
    public class GenericBasicTests
    {
        [Fact]
        public void ModbusDataConverter_GetRegisterCount_BasicTypes_ReturnsCorrectValues()
        {
            // Act & Assert
            Assert.Equal(1, ModbusDataConverter.GetRegisterCount<byte>());
            Assert.Equal(1, ModbusDataConverter.GetRegisterCount<ushort>());
            Assert.Equal(2, ModbusDataConverter.GetRegisterCount<int>());
            Assert.Equal(2, ModbusDataConverter.GetRegisterCount<float>());
            Assert.Equal(4, ModbusDataConverter.GetRegisterCount<double>());
        }
        
        [Fact]
        public void ModbusDataConverter_GetTotalRegisterCount_MultipleElements_ReturnsCorrectValues()
        {
            // Act & Assert
            Assert.Equal(5, ModbusDataConverter.GetTotalRegisterCount<byte>(10)); // 10 bytes = 5 registers
            Assert.Equal(10, ModbusDataConverter.GetTotalRegisterCount<int>(5)); // 5 ints = 10 registers
            Assert.Equal(20, ModbusDataConverter.GetTotalRegisterCount<double>(5)); // 5 doubles = 20 registers
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public void ModbusDataConverter_ByteArray_RoundTrip_Success(ModbusEndianness endianness)
        {
            // Arrange
            var original = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(original, endianness);
            var result = ModbusDataConverter.FromBytes<byte>(bytes, original.Length, endianness);
            
            // Assert
            Assert.Equal(original, result);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public void ModbusDataConverter_IntArray_RoundTrip_Success(ModbusEndianness endianness)
        {
            // Arrange
            var original = new int[] { 123456789, -987654321, 0 };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(original, endianness);
            var result = ModbusDataConverter.FromBytes<int>(bytes, original.Length, endianness);
            
            // Assert
            Assert.Equal(original, result);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public void ModbusDataConverter_FloatArray_RoundTrip_Success(ModbusEndianness endianness)
        {
            // Arrange
            var original = new float[] { 3.14159f, -2.71828f, 100.5f };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(original, endianness);
            var result = ModbusDataConverter.FromBytes<float>(bytes, original.Length, endianness);
            
            // Assert
            Assert.Equal(original.Length, result.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], result[i], 5); // 5位精度
            }
        }
        
        [Fact]
        public void ModbusDataConverter_EmptyArray_HandledCorrectly()
        {
            // Arrange
            var emptyArray = Array.Empty<int>();
            
            // Act
            var result = ModbusDataConverter.ToBytes(emptyArray, ModbusEndianness.BigEndian);
            
            // Assert
            Assert.Empty(result);
        }
        
        [Fact]
        public void ModbusSpanExtensions_BasicOperations_Success()
        {
            // Arrange
            var buffer = new ushort[4];
            var span = buffer.AsSpan();
            const int testValue = 123456789;
            
            // Act - 设置大端格式的值
            span.SetBigEndian<int>(0, testValue);
            var readValue = span.GetBigEndian<int>(0);
            
            // Assert
            Assert.Equal(testValue, readValue);
        }
        
        [Fact]
        public void ModbusSpanExtensions_LittleEndian_BasicOperations_Success()
        {
            // Arrange
            var buffer = new ushort[4];
            var span = buffer.AsSpan();
            const int testValue = 123456789;
            
            // Act - 设置小端格式的值
            span.SetLittleEndian<int>(0, testValue);
            var readValue = span.GetLittleEndian<int>(0);
            
            // Assert
            Assert.Equal(testValue, readValue);
        }
        
        [Fact]
        public void ModbusDataConverter_DifferentEndianness_ProducesDifferentResults()
        {
            // Arrange
            var originalValue = new int[] { 305419896 }; // 0x12345678
            
            // Act
            var bigEndianBytes = ModbusDataConverter.ToBytes(originalValue, ModbusEndianness.BigEndian);
            var littleEndianBytes = ModbusDataConverter.ToBytes(originalValue, ModbusEndianness.LittleEndian);
            var midLittleEndianBytes = ModbusDataConverter.ToBytes(originalValue, ModbusEndianness.MidLittleEndian);
            
            // Assert - 不同字节序应该产生不同的字节数组（除非值特殊）
            Assert.NotEqual(bigEndianBytes, littleEndianBytes);
            Assert.NotEqual(bigEndianBytes, midLittleEndianBytes);
            Assert.NotEqual(littleEndianBytes, midLittleEndianBytes);
        }
    }
}