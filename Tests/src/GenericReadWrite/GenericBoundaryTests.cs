using Xunit;
using ModbusLib.Models;
using ModbusLib.Enums;
using ModbusLib.Tests;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// 泛型功能边界情况和异常处理测试
    /// </summary>
    public class GenericBoundaryTests : IDisposable
    {
        private readonly TestModbusClient _client;
        
        public GenericBoundaryTests()
        {
            _client = new TestModbusClient();
        }
        
        #region 数据转换边界测试
        
        [Fact]
        public void ModbusDataConverter_EmptyArray_DoesNotThrow()
        {
            // Arrange
            var emptyArray = Array.Empty<int>();
            
            // Act & Assert - 空数组应该返回空字节数组
            var result = ModbusDataConverter.ToBytes(emptyArray, ModbusEndianness.BigEndian);
            Assert.Empty(result);
        }
        
        [Fact]
        public void ModbusDataConverter_NullArray_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ModbusDataConverter.ToBytes<int>(null!, ModbusEndianness.BigEndian));
        }
        
        [Fact]
        public void ModbusDataConverter_FromBytes_EmptyBytes_ThrowsException()
        {
            // Arrange
            var emptyBytes = Array.Empty<byte>();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ModbusDataConverter.FromBytes<int>(emptyBytes, 1, ModbusEndianness.BigEndian));
        }
        
        [Fact]
        public void ModbusDataConverter_FromBytes_NullBytes_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ModbusDataConverter.FromBytes<int>(null!, 1, ModbusEndianness.BigEndian));
        }
        
        [Fact]
        public void ModbusDataConverter_FromBytes_ZeroCount_ReturnsEmptyArray()
        {
            // Arrange
            var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            
            // Act
            var result = ModbusDataConverter.FromBytes<int>(bytes, 0, ModbusEndianness.BigEndian);
            
            // Assert
            Assert.Empty(result);
        }
        
        [Fact]
        public void ModbusDataConverter_FromBytes_NegativeCount_ThrowsException()
        {
            // Arrange
            var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ModbusDataConverter.FromBytes<int>(bytes, -1, ModbusEndianness.BigEndian));
        }
        
        [Fact]
        public void ModbusDataConverter_MaxArraySize_DoesNotThrow()
        {
            // Arrange - 测试较大数组（但不超过系统限制）
            const int maxTestSize = 1000; // 测试用的最大大小
            var largeArray = new byte[maxTestSize];
            for (int i = 0; i < maxTestSize; i++)
            {
                largeArray[i] = (byte)(i % 256);
            }
            
            // Act & Assert - 不应该抛出异常
            var bytes = ModbusDataConverter.ToBytes(largeArray, ModbusEndianness.BigEndian);
            var result = ModbusDataConverter.FromBytes<byte>(bytes, maxTestSize, ModbusEndianness.BigEndian);
            
            Assert.Equal(maxTestSize, result.Length);
            Assert.Equal(largeArray, result);
        }
        
        #endregion
        
        #region 寄存器计数边界测试
        
        [Fact]
        public void GetTotalRegisterCount_ZeroCount_ReturnsZero()
        {
            // Act
            var result = ModbusDataConverter.GetTotalRegisterCount<int>(0);
            
            // Assert
            Assert.Equal(0, result);
        }
        
        [Fact]
        public void GetTotalRegisterCount_NegativeCount_ReturnsZero()
        {
            // Act
            var result = ModbusDataConverter.GetTotalRegisterCount<int>(-1);
            
            // Assert
            Assert.Equal(0, result);
        }
        
        [Fact]
        public void GetTotalRegisterCount_LargeCount_ReturnsCorrectValue()
        {
            // Arrange
            const int largeCount = 1000;
            
            // Act
            var byteResult = ModbusDataConverter.GetTotalRegisterCount<byte>(largeCount);
            var intResult = ModbusDataConverter.GetTotalRegisterCount<int>(largeCount);
            
            // Assert
            Assert.Equal(500, byteResult); // 1000 bytes = 500 registers
            Assert.Equal(2000, intResult); // 1000 ints = 2000 registers
        }
        
        [Theory]
        [InlineData(1, 1)]     // 1 byte = 1 register
        [InlineData(2, 1)]     // 2 bytes = 1 register  
        [InlineData(3, 2)]     // 3 bytes = 2 registers
        [InlineData(4, 2)]     // 4 bytes = 2 registers
        [InlineData(5, 3)]     // 5 bytes = 3 registers
        public void GetTotalRegisterCount_EdgeCases_ReturnsCorrectValue(int byteCount, int expectedRegisters)
        {
            // Act
            var result = ModbusDataConverter.GetTotalRegisterCount<byte>(byteCount);
            
            // Assert
            Assert.Equal(expectedRegisters, result);
        }
        
        #endregion
        
        #region 客户端边界测试
        
        [Fact]
        public async Task ReadHoldingRegisters_ZeroCount_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.ReadHoldingRegistersAsync<int>(1, 100, 0));
        }
        
        [Fact]
        public async Task ReadInputRegisters_ZeroCount_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.ReadInputRegistersAsync<float>(1, 100, 0));
        }
        
        [Fact]
        public async Task WriteMultipleRegisters_EmptyArray_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.WriteMultipleRegistersAsync<int>(1, 100, Array.Empty<int>()));
        }
        
        [Fact]
        public async Task WriteMultipleRegisters_NullArray_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.WriteMultipleRegistersAsync<int>(1, 100, null!));
        }
        
        [Fact]
        public async Task WriteMultipleRegisters_ExceedsRegisterLimit_ThrowsArgumentException()
        {
            // Arrange - 创建需要超过123个寄存器的数组
            var largeArray = new byte[250]; // 需要125个寄存器，超过限制
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.WriteMultipleRegistersAsync<byte>(1, 100, largeArray));
        }
        
        [Fact]
        public async Task WriteMultipleRegisters_ExactlyAtLimit_Success()
        {
            // Arrange - 创建恰好需要123个寄存器的数组
            var maxArray = new byte[246]; // 需要123个寄存器
            _client.SetupWriteMultipleRegistersResponse(1, 100);
            
            // Act & Assert - 不应该抛出异常
            await _client.WriteMultipleRegistersAsync<byte>(1, 100, maxArray);
            
            // Verify the call was made
            Assert.True(_client.WriteMultipleRegistersCalled);
        }
        
        #endregion
        
        #region Span扩展边界测试
        
        [Fact]
        public void SpanExtensions_GetBigEndian_InvalidAddress_ThrowsException()
        {
            // Arrange
            var buffer = new ushort[2];
            
            // Act & Assert - 访问超出范围的地址
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                buffer.AsSpan().GetBigEndian<long>(0)); // long需要4个寄存器，但只有2个
        }
        
        [Fact]
        public void SpanExtensions_SetBigEndian_InvalidAddress_ThrowsException()
        {
            // Arrange
            var buffer = new ushort[1];
            
            // Act & Assert - 设置超出范围的地址
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                buffer.AsSpan().SetBigEndian<int>(0, 12345)); // int需要2个寄存器，但只有1个
        }
        
        [Fact]
        public void SpanExtensions_GetLittleEndian_NegativeAddress_ThrowsException()
        {
            // Arrange
            var buffer = new ushort[4];
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                buffer.AsSpan().GetLittleEndian<int>(-1));
        }
        
        [Fact]
        public void SpanExtensions_SetLittleEndian_NegativeAddress_ThrowsException()
        {
            // Arrange
            var buffer = new ushort[4];
            
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                buffer.AsSpan().SetLittleEndian<int>(-1, 12345));
        }
        
        [Fact]
        public void SpanExtensions_EdgeAddress_Success()
        {
            // Arrange - 测试边界地址
            var buffer = new ushort[4];
            var span = buffer.AsSpan();
            const int testValue = 0x12345678;
            
            // Act - 在最后可能的位置设置int值
            span.SetBigEndian<int>(2, testValue); // 地址2，使用寄存器2和3
            var result = span.GetBigEndian<int>(2);
            
            // Assert
            Assert.Equal(testValue, result);
        }
        
        #endregion
        
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}