using Xunit;
using ModbusLib.Models;
using ModbusLib.Enums;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// ModbusDataConverter单元测试
    /// </summary>
    public class ModbusDataConverterTests
    {
        [Fact]
        public void GetRegisterCount_Byte_ReturnsOne()
        {
            // Act
            var count = ModbusDataConverter.GetRegisterCount<byte>();
            
            // Assert
            Assert.Equal(1, count);
        }
        
        [Fact]
        public void GetRegisterCount_UShort_ReturnsOne()
        {
            // Act
            var count = ModbusDataConverter.GetRegisterCount<ushort>();
            
            // Assert
            Assert.Equal(1, count);
        }
        
        [Fact]
        public void GetRegisterCount_Int_ReturnsTwo()
        {
            // Act
            var count = ModbusDataConverter.GetRegisterCount<int>();
            
            // Assert
            Assert.Equal(2, count);
        }
        
        [Fact]
        public void GetRegisterCount_Float_ReturnsTwo()
        {
            // Act
            var count = ModbusDataConverter.GetRegisterCount<float>();
            
            // Assert
            Assert.Equal(2, count);
        }
        
        [Fact]
        public void GetRegisterCount_Double_ReturnsFour()
        {
            // Act
            var count = ModbusDataConverter.GetRegisterCount<double>();
            
            // Assert
            Assert.Equal(4, count);
        }
        
        [Fact]
        public void GetTotalRegisterCount_MultipleBytes_ReturnsCorrectCount()
        {
            // Act
            var count = ModbusDataConverter.GetTotalRegisterCount<byte>(10);
            
            // Assert
            Assert.Equal(5, count); // 10 bytes需要5个寄存器
        }
        
        [Fact]
        public void GetTotalRegisterCount_MultipleInts_ReturnsCorrectCount()
        {
            // Act
            var count = ModbusDataConverter.GetTotalRegisterCount<int>(5);
            
            // Assert
            Assert.Equal(10, count); // 5个int需要10个寄存器
        }
        
        [Theory]
        [InlineData(new byte[] { 0x12, 0x34 }, ModbusEndianness.BigEndian)]
        [InlineData(new byte[] { 0xAB, 0xCD }, ModbusEndianness.LittleEndian)]
        public void ToBytes_ByteArray_ReturnsCorrectBytes(byte[] input, ModbusEndianness endianness)
        {
            // Act
            var result = ModbusDataConverter.ToBytes(input, endianness);
            
            // Assert
            Assert.Equal(input.Length, result.Length);
        }
        
        [Fact]
        public void ToBytes_IntArray_BigEndian_ReturnsCorrectBytes()
        {
            // Arrange
            var values = new int[] { unchecked((int)0x12345678) };
            
            // Act
            var result = ModbusDataConverter.ToBytes(values, ModbusEndianness.BigEndian);
            
            // Assert
            Assert.Equal(4, result.Length);
            if (BitConverter.IsLittleEndian)
            {
                // 大端序应该反转字节顺序
                Assert.Equal((byte)0x78, result[0]);
                Assert.Equal((byte)0x56, result[1]);
                Assert.Equal((byte)0x34, result[2]);
                Assert.Equal((byte)0x12, result[3]);
            }
        }
        
        [Fact]
        public void FromBytes_ToByteArray_RoundTrip_Success()
        {
            // Arrange
            var original = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(original, ModbusEndianness.BigEndian);
            var result = ModbusDataConverter.FromBytes<byte>(bytes, original.Length, ModbusEndianness.BigEndian);
            
            // Assert
            Assert.Equal(original, result);
        }
        
        [Fact]
        public void FromBytes_ToIntArray_RoundTrip_Success()
        {
            // Arrange
            var original = new int[] { unchecked((int)0x12345678), unchecked((int)0x87654321) };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(original, ModbusEndianness.BigEndian);
            var result = ModbusDataConverter.FromBytes<int>(bytes, original.Length, ModbusEndianness.BigEndian);
            
            // Assert
            Assert.Equal(original, result);
        }
        
        [Fact]
        public void FromBytes_ToFloatArray_RoundTrip_Success()
        {
            // Arrange
            var original = new float[] { 3.14159f, -2.71828f };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(original, ModbusEndianness.BigEndian);
            var result = ModbusDataConverter.FromBytes<float>(bytes, original.Length, ModbusEndianness.BigEndian);
            
            // Assert
            Assert.Equal(original.Length, result.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], result[i], 5); // 5位精度
            }
        }
        
        [Fact]
        public void FromBytes_InsufficientData_ThrowsArgumentException()
        {
            // Arrange
            var bytes = new byte[] { 0x12, 0x34 }; // 只有2字节，但需要4字节用于int
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ModbusDataConverter.FromBytes<int>(bytes, 1, ModbusEndianness.BigEndian));
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        [InlineData(ModbusEndianness.MidLittleEndian)]
        public void ToBytes_DifferentEndianness_DoesNotThrow(ModbusEndianness endianness)
        {
            // Arrange
            var values = new int[] { unchecked((int)0x12345678) };
            
            // Act & Assert (不应该抛出异常)
            var result = ModbusDataConverter.ToBytes(values, endianness);
            Assert.Equal(4, result.Length);
        }
    }
}