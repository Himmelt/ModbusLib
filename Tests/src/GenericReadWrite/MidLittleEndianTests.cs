using Xunit;
using ModbusLib.Models;
using ModbusLib.Enums;
using ModbusLib.Tests;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// 中端字节序（MidLittleEndian）专项测试
    /// </summary>
    public class MidLittleEndianTests : IDisposable
    {
        private readonly TestModbusClient _client;
        
        public MidLittleEndianTests()
        {
            _client = new TestModbusClient();
        }
        
        [Fact]
        public void ModbusDataConverter_MidLittleEndian_SingleInt_CorrectConversion()
        {
            // Arrange
            var originalValue = new int[] { unchecked((int)0x12345678) };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValue, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<int>(bytes, 1, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Single(result);
            Assert.Equal(originalValue[0], result[0]);
        }
        
        [Fact]
        public void ModbusDataConverter_MidLittleEndian_MultipleInts_CorrectConversion()
        {
            // Arrange
            var originalValues = new int[] { unchecked((int)0x12345678), unchecked((int)0x87654321), unchecked((int)0xABCDEF01) };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValues, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<int>(bytes, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, result.Length);
            Assert.Equal(originalValues, result);
        }
        
        [Fact]
        public void ModbusDataConverter_MidLittleEndian_Float_CorrectConversion()
        {
            // Arrange
            var originalValues = new float[] { 3.14159f, -2.71828f, 123.456f };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValues, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<float>(bytes, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, result.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i], result[i], 5); // 5位精度
            }
        }
        
        [Theory]
        [InlineData(unchecked((int)0x12345678))]
        [InlineData(unchecked((int)0x87654321))]
        [InlineData(unchecked((int)0xABCDEF01))]
        [InlineData(-1)]
        [InlineData(0)]
        public void ModbusDataConverter_MidLittleEndian_SpecificValues_RoundTrip(int testValue)
        {
            // Arrange
            var originalValues = new int[] { testValue };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValues, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<int>(bytes, 1, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Single(result);
            Assert.Equal(testValue, result[0]);
        }
        
        [Fact]
        public void ModbusDataConverter_CompareDifferentEndianness_ProducesDifferentResults()
        {
            // Arrange
            var originalValue = new int[] { unchecked((int)0x12345678) };
            
            // Act
            var bigEndianBytes = ModbusDataConverter.ToBytes(originalValue, ModbusEndianness.BigEndian);
            var littleEndianBytes = ModbusDataConverter.ToBytes(originalValue, ModbusEndianness.LittleEndian);
            var midLittleEndianBytes = ModbusDataConverter.ToBytes(originalValue, ModbusEndianness.MidLittleEndian);
            
            // Assert - 不同字节序应该产生不同的字节数组（除非值特殊）
            Assert.NotEqual(bigEndianBytes, littleEndianBytes);
            Assert.NotEqual(bigEndianBytes, midLittleEndianBytes);
            Assert.NotEqual(littleEndianBytes, midLittleEndianBytes);
        }
        
        [Fact]
        public async Task ReadWriteHoldingRegisters_MidLittleEndian_IntArray_Success()
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = 100;
            var originalValues = new int[] { unchecked((int)0x12345678), unchecked((int)0x87654321) };
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<int>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, ModbusEndianness.MidLittleEndian));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<int>(slaveId, startAddress, originalValues, ModbusEndianness.MidLittleEndian);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<int>(slaveId, startAddress, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            Assert.Equal(originalValues, readValues);
        }
        
        [Fact]
        public async Task ReadWriteHoldingRegisters_MidLittleEndian_FloatArray_Success()
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = 200;
            var originalValues = new float[] { 3.14159f, -2.71828f, 100.5f };
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<float>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, ModbusEndianness.MidLittleEndian));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<float>(slaveId, startAddress, originalValues, ModbusEndianness.MidLittleEndian);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<float>(slaveId, startAddress, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i], readValues[i], 5); // 5位精度
            }
        }
        
        [Fact]
        public async Task WriteSingleRegister_MidLittleEndian_Int_UsesMultipleRegisters()
        {
            // Arrange
            byte slaveId = 1;
            ushort address = 300;
            int value = unchecked((int)0x12345678);
            
            _client.SetupWriteMultipleRegistersResponse(slaveId, address);
            
            // Act
            await _client.WriteSingleRegisterAsync<int>(slaveId, address, value, ModbusEndianness.MidLittleEndian);
            
            // Assert - 验证调用了WriteMultipleRegisters
            Assert.True(_client.WriteMultipleRegistersCalled);
            Assert.False(_client.WriteSingleRegisterCalled);
        }
        
        [Fact]
        public void ModbusDataConverter_MidLittleEndian_ByteArray_HandledCorrectly()
        {
            // Arrange - 字节数组对于中端字节序的特殊情况
            var originalValues = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValues, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<byte>(bytes, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert - 字节数组应该正确往返转换
            Assert.Equal((ushort)originalValues.Length, result.Length);
            Assert.Equal(originalValues, result);
        }
        
        [Fact]
        public void ModbusDataConverter_MidLittleEndian_UShortArray_HandledCorrectly()
        {
            // Arrange - 2字节值对于中端字节序
            var originalValues = new ushort[] { 0x1234, 0x5678, 0xABCD };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValues, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<ushort>(bytes, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, result.Length);
            Assert.Equal(originalValues, result);
        }
        
        [Fact]
        public void ModbusDataConverter_MidLittleEndian_Double_HandledCorrectly()
        {
            // Arrange - 8字节值对于中端字节序
            var originalValues = new double[] { Math.PI, Math.E };
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValues, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<double>(bytes, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, result.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i], result[i], 10); // 10位精度
            }
        }
        
        [Fact]
        public void ModbusDataConverter_MidLittleEndian_OddByteCount_HandledCorrectly()
        {
            // Arrange - 奇数字节数的数据
            var originalValues = new byte[] { 0x12, 0x34, 0x56 }; // 3字节
            
            // Act
            var bytes = ModbusDataConverter.ToBytes(originalValues, ModbusEndianness.MidLittleEndian);
            var result = ModbusDataConverter.FromBytes<byte>(bytes, (ushort)originalValues.Length, ModbusEndianness.MidLittleEndian);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, result.Length);
            Assert.Equal(originalValues, result);
        }
        
        /// <summary>
        /// 将泛型数组转换为ushort寄存器数组
        /// </summary>
        private static ushort[] ConvertToRegisters<T>(T[] values, ModbusEndianness endianness) where T : unmanaged
        {
            var bytes = ModbusDataConverter.ToBytes(values, endianness);
            return TestHelper.BytesToUshortArray(bytes);
        }
        
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}