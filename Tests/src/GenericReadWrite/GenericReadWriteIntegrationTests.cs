using Xunit;
using ModbusLib.Enums;
using ModbusLib.Factories;
using ModbusLib.Models;
using ModbusLib.Tests;

namespace ModbusLib.Tests.GenericReadWrite
{
    /// <summary>
    /// 泛型读写功能集成测试
    /// </summary>
    public class GenericReadWriteIntegrationTests : IDisposable
    {
        private readonly TestModbusClient _client;
        
        public GenericReadWriteIntegrationTests()
        {
            // 创建测试客户端
            var config = new NetworkConnectionConfig
            {
                Host = "127.0.0.1",
                Port = 502
            };
            _client = new TestModbusClient();
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        public async Task ReadWriteHoldingRegisters_ByteArray_Success(ModbusEndianness endianness)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = 100;
            var originalValues = new byte[] { 0x12, 0x34, 0x56, 0x78, 0xAB, 0xCD };
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<byte>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<byte>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<byte>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            Assert.Equal(originalValues, readValues);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        public async Task ReadWriteHoldingRegisters_IntArray_Success(ModbusEndianness endianness)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = 200;
            var originalValues = new int[] { 0x12345678, -987654321, 0 };
            
            // 模拟设置寄存器数据
            _client.SetupReadHoldingRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<int>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
                
            _client.SetupWriteMultipleRegistersResponse(slaveId, startAddress);
            
            // Act - Write
            await _client.WriteMultipleRegistersAsync<int>(slaveId, startAddress, originalValues, endianness);
            
            // Act - Read
            var readValues = await _client.ReadHoldingRegistersAsync<int>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            Assert.Equal(originalValues, readValues);
        }
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        public async Task ReadWriteHoldingRegisters_FloatArray_Success(ModbusEndianness endianness)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = 300;
            var originalValues = new float[] { 3.14159f, -2.71828f, 0.0f };
            
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
        
        [Theory]
        [InlineData(ModbusEndianness.BigEndian)]
        [InlineData(ModbusEndianness.LittleEndian)]
        public async Task ReadWriteInputRegisters_DoubleArray_Success(ModbusEndianness endianness)
        {
            // Arrange
            byte slaveId = 1;
            ushort startAddress = 400;
            var originalValues = new double[] { Math.PI, Math.E };
            
            // 模拟设置寄存器数据  
            _client.SetupReadInputRegistersResponse(slaveId, startAddress, 
                (ushort)ModbusDataConverter.GetTotalRegisterCount<double>((ushort)originalValues.Length), 
                ConvertToRegisters(originalValues, endianness));
            
            // Act - Read
            var readValues = await _client.ReadInputRegistersAsync<double>(slaveId, startAddress, (ushort)originalValues.Length, endianness);
            
            // Assert
            Assert.Equal((ushort)originalValues.Length, readValues.Length);
            for (int i = 0; i < (ushort)originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i], readValues[i], 10); // 10位精度
            }
        }
        
        [Fact]
        public async Task WriteSingleRegister_UShort_Success()
        {
            // Arrange
            byte slaveId = 1;
            ushort address = 500;
            ushort value = 0x1234;
            
            _client.SetupWriteSingleRegisterResponse(slaveId, address);
            
            // Act
            await _client.WriteSingleRegisterAsync<ushort>(slaveId, address, value);
            
            // Assert - 验证调用
            Assert.True(_client.WriteSingleRegisterCalled);
        }
        
        [Fact]
        public async Task WriteSingleRegister_Int_UsesMultipleRegisters()
        {
            // Arrange
            byte slaveId = 1;
            ushort address = 600;
            int value = 0x12345678;
            
            _client.SetupWriteMultipleRegistersResponse(slaveId, address);
            
            // Act
            await _client.WriteSingleRegisterAsync<int>(slaveId, address, value, ModbusEndianness.BigEndian);
            
            // Assert - 验证调用了WriteMultipleRegisters而不是WriteSingleRegister
            Assert.True(_client.WriteMultipleRegistersCalled);
            Assert.False(_client.WriteSingleRegisterCalled);
        }
        
        [Fact]
        public async Task ReadHoldingRegisters_InvalidCount_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.ReadHoldingRegistersAsync<int>(1, 100, 0));
        }
        
        [Fact]
        public async Task WriteMultipleRegisters_EmptyArray_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.WriteMultipleRegistersAsync<int>(1, 100, new int[0]));
        }
        
        [Fact]
        public async Task WriteMultipleRegisters_TooManyRegisters_ThrowsArgumentException()
        {
            // Arrange
            var largeArray = new byte[300]; // 需要150个寄存器，超过限制(123)
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _client.WriteMultipleRegistersAsync<byte>(1, 100, largeArray));
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