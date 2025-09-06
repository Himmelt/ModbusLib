using Xunit;
using ModbusLib.Models;
using System;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// ModbusSpanExtensions单元测试
    /// </summary>
    public class ModbusSpanExtensionsTests
    {
        [Fact]
        public void GetBigEndian_Byte_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[] { 0x1234 }.AsSpan();
            
            // Act
            var result = buffer.GetBigEndian<byte>(0);
            
            // Assert
            Assert.Equal(0x12, result);
        }
        
        [Fact]
        public void GetBigEndian_UShort_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[] { 0x1234 }.AsSpan();
            
            // Act
            var result = buffer.GetBigEndian<ushort>(0);
            
            // Assert
            Assert.Equal(0x1234, result);
        }
        
        [Fact]
        public void GetBigEndian_Int_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[] { 0x1234, 0x5678 }.AsSpan();
            
            // Act
            var result = buffer.GetBigEndian<int>(0);
            
            // Assert
            // 大端序: 0x1234 5678 -> 高字节在前
            Assert.Equal(0x12345678, result);
        }
        
        [Fact]
        public void GetLittleEndian_Byte_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[] { 0x1234 }.AsSpan();
            
            // Act
            var result = buffer.GetLittleEndian<byte>(0);
            
            // Assert
            Assert.Equal(0x34, result);
        }
        
        [Fact]
        public void GetLittleEndian_UShort_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[] { 0x1234 }.AsSpan();
            
            // Act
            var result = buffer.GetLittleEndian<ushort>(0);
            
            // Assert
            Assert.Equal(0x3412, result);
        }
        
        [Fact]
        public void GetLittleEndian_Int_ReturnsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[] { 0x1234, 0x5678 }.AsSpan();
            
            // Act
            var result = buffer.GetLittleEndian<int>(0);
            
            // Assert
            // 小端序: 0x1234 5678 -> 低字节在前
            Assert.Equal(0x78563412, result);
        }
        
        [Fact]
        public void SetBigEndian_Byte_SetsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[1];
            var span = buffer.AsSpan();
            
            // Act
            span.SetBigEndian<byte>(0, 0xAB);
            
            // Assert
            Assert.Equal(0xAB00, buffer[0]);
        }
        
        [Fact]
        public void SetBigEndian_UShort_SetsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[1];
            var span = buffer.AsSpan();
            
            // Act
            span.SetBigEndian<ushort>(0, 0x1234);
            
            // Assert
            Assert.Equal(0x1234, buffer[0]);
        }
        
        [Fact]
        public void SetBigEndian_Int_SetsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[2];
            var span = buffer.AsSpan();
            
            // Act
            span.SetBigEndian<int>(0, 0x12345678);
            
            // Assert
            Assert.Equal(0x1234, buffer[0]);
            Assert.Equal(0x5678, buffer[1]);
        }
        
        [Fact]
        public void SetLittleEndian_Byte_SetsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[1];
            var span = buffer.AsSpan();
            
            // Act
            span.SetLittleEndian<byte>(0, 0xAB);
            
            // Assert
            Assert.Equal(0x00AB, buffer[0]);
        }
        
        [Fact]
        public void SetLittleEndian_UShort_SetsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[1];
            var span = buffer.AsSpan();
            
            // Act
            span.SetLittleEndian<ushort>(0, 0x1234);
            
            // Assert
            Assert.Equal(0x3412, buffer[0]);
        }
        
        [Fact]
        public void SetLittleEndian_Int_SetsCorrectValue()
        {
            // Arrange
            var buffer = new ushort[2];
            var span = buffer.AsSpan();
            
            // Act
            span.SetLittleEndian<int>(0, 0x12345678);
            
            // Assert
            Assert.Equal(0x7856, buffer[0]);
            Assert.Equal(0x1234, buffer[1]);
        }
        
        [Fact]
        public void GetBigEndian_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var buffer = new ushort[1];
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            {
                var span = buffer.AsSpan();
                return span.GetBigEndian<int>(0); // int需要2个寄存器，但只有1个
            });
        }
        
        [Fact]
        public void SetBigEndian_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var buffer = new ushort[1];
            
            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            {
                var span = buffer.AsSpan();
                span.SetBigEndian<int>(0, 0x12345678); // int需要2个寄存器，但只有1个
            });
        }
        
        [Fact]
        public void GetSetRoundTrip_BigEndian_Success()
        {
            // Arrange
            var buffer = new ushort[4].AsSpan();
            var originalValue = 3.14159f;
            
            // Act
            buffer.SetBigEndian<float>(0, originalValue);
            var result = buffer.GetBigEndian<float>(0);
            
            // Assert
            Assert.Equal(originalValue, result);
        }
        
        [Fact]
        public void GetSetRoundTrip_LittleEndian_Success()
        {
            // Arrange
            var buffer = new ushort[4].AsSpan();
            var originalValue = 2.71828;
            
            // Act
            buffer.SetLittleEndian<double>(0, originalValue);
            var result = buffer.GetLittleEndian<double>(0);
            
            // Assert
            Assert.Equal(originalValue, result, 10); // 10位精度
        }
    }
}