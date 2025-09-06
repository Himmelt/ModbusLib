using FluentAssertions;
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using Moq;
using Xunit;

namespace ModbusLib.Tests.WriteFunctions;

/// <summary>
/// Modbus 写入功能测试
/// </summary>
public class ModbusWriteTests
{
    private readonly Mock<IModbusTransport> _mockTransport;
    private readonly Mock<IModbusProtocol> _mockProtocol;
    private readonly TestableModbusClient _client;

    public ModbusWriteTests()
    {
        _mockTransport = new Mock<IModbusTransport>();
        _mockProtocol = new Mock<IModbusProtocol>();
        _client = new TestableModbusClient(_mockTransport.Object, _mockProtocol.Object);
        
        // 设置默认的连接状态
        _mockTransport.Setup(t => t.IsConnected).Returns(true);
        _mockTransport.Setup(t => t.Timeout).Returns(TestHelper.DefaultTimeout);
    }

    #region WriteSingleCoil Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteSingleCoilAsync_ValidValues_Success(bool value)
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort address = 100;
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteSingleCoil);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteSingleCoilAsync(slaveId, address, value);

        // Assert
        VerifyWriteRequest(slaveId, ModbusFunction.WriteSingleCoil, address, 1);
    }

    [Fact]
    public async Task WriteSingleCoilAsync_DataTransformation_CorrectFormat()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort address = 50;
        const bool value = true;
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteSingleCoil);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteSingleCoilAsync(slaveId, address, value);

        // Assert
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.SlaveId == slaveId &&
            r.Function == ModbusFunction.WriteSingleCoil &&
            r.StartAddress == address &&
            r.Quantity == 1 &&
            r.Data != null &&
            r.Data.Length == 1 &&
            r.Data[0] == 1)), Times.Once);
    }

    [Fact]
    public async Task WriteSingleCoilAsync_FalseValue_CorrectData()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort address = 50;
        const bool value = false;
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteSingleCoil);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteSingleCoilAsync(slaveId, address, value);

        // Assert
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.Data != null &&
            r.Data.Length == 1 &&
            r.Data[0] == 0)), Times.Once);
    }

    [Fact]
    public async Task WriteSingleCoilAsync_ErrorResponse_ThrowsModbusException()
    {
        // Arrange
        var errorResponse = ModbusResponse.CreateError(TestHelper.TestSlaveId, 
            ModbusFunction.WriteSingleCoil, ModbusExceptionCode.IllegalDataAddress);
        
        SetupSuccessfulRequest(errorResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModbusException>(() => 
            _client.WriteSingleCoilAsync(TestHelper.TestSlaveId, 100, true));
        
        exception.ExceptionCode.Should().Be(ModbusExceptionCode.IllegalDataAddress);
        exception.Function.Should().Be(ModbusFunction.WriteSingleCoil);
    }

    #endregion

    #region WriteSingleRegister Tests

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(32767)]
    [InlineData(65535)]
    public async Task WriteSingleRegisterAsync_ValidValues_Success(ushort value)
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort address = 200;
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteSingleRegister);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteSingleRegisterAsync(slaveId, address, value);

        // Assert
        VerifyWriteRequest(slaveId, ModbusFunction.WriteSingleRegister, address, 1);
    }

    [Fact]
    public async Task WriteSingleRegisterAsync_DataFormat_BigEndian()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort address = 300;
        const ushort value = 0x1234; // Test specific value for byte order
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteSingleRegister);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteSingleRegisterAsync(slaveId, address, value);

        // Assert
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.Data != null &&
            r.Data.Length == 2 &&
            r.Data[0] == 0x12 && // High byte first (big-endian)
            r.Data[1] == 0x34)), Times.Once);
    }

    #endregion

    #region WriteMultipleCoils Tests

    [Fact]
    public async Task WriteMultipleCoilsAsync_ValidArray_Success()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = 100;
        var values = new bool[] { true, false, true, false, true, false, true, true };
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteMultipleCoils);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteMultipleCoilsAsync(slaveId, startAddress, values);

        // Assert
        VerifyWriteRequest(slaveId, ModbusFunction.WriteMultipleCoils, startAddress, (ushort)values.Length);
    }

    [Fact]
    public async Task WriteMultipleCoilsAsync_DataConversion_CorrectByteArray()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = 0;
        var values = new bool[] { true, false, true, false, true, false, true, true }; // 0xB5
        
        var expectedBytes = ModbusUtils.BoolArrayToByteArray(values);
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteMultipleCoils);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteMultipleCoilsAsync(slaveId, startAddress, values);

        // Assert
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.Data != null &&
            r.Data.SequenceEqual(expectedBytes))), Times.Once);
    }

    [Fact]
    public async Task WriteMultipleCoilsAsync_LargeArray_Success()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = 0;
        var values = TestHelper.CreateTestBoolArray(1000, true); // Large but valid array
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteMultipleCoils);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteMultipleCoilsAsync(slaveId, startAddress, values);

        // Assert
        VerifyWriteRequest(slaveId, ModbusFunction.WriteMultipleCoils, startAddress, (ushort)values.Length);
    }

    [Fact]
    public async Task WriteMultipleCoilsAsync_EmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var emptyArray = new bool[0];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.WriteMultipleCoilsAsync(TestHelper.TestSlaveId, 0, emptyArray));
    }

    [Fact]
    public async Task WriteMultipleCoilsAsync_NullArray_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.WriteMultipleCoilsAsync(TestHelper.TestSlaveId, 0, null!));
    }

    [Fact]
    public async Task WriteMultipleCoilsAsync_TooManyCoils_ThrowsArgumentException()
    {
        // Arrange
        var tooManyCoils = TestHelper.CreateTestBoolArray(1969, true); // Exceeds limit of 1968

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.WriteMultipleCoilsAsync(TestHelper.TestSlaveId, 0, tooManyCoils));
    }

    #endregion

    #region WriteMultipleRegisters Tests

    [Fact]
    public async Task WriteMultipleRegistersAsync_ValidArray_Success()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = 200;
        var values = TestHelper.CreateTestUshortArray(5, 1000);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteMultipleRegisters);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteMultipleRegistersAsync(slaveId, startAddress, values);

        // Assert
        VerifyWriteRequest(slaveId, ModbusFunction.WriteMultipleRegisters, startAddress, (ushort)values.Length);
    }

    [Fact]
    public async Task WriteMultipleRegistersAsync_DataConversion_CorrectByteArray()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = 0;
        var values = new ushort[] { 0x1234, 0x5678, 0x9ABC };
        
        var expectedBytes = ModbusUtils.UshortArrayToByteArray(values);
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteMultipleRegisters);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteMultipleRegistersAsync(slaveId, startAddress, values);

        // Assert
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.Data != null &&
            r.Data.SequenceEqual(expectedBytes))), Times.Once);
    }

    [Fact]
    public async Task WriteMultipleRegistersAsync_MaximumQuantity_Success()
    {
        // Arrange - Test maximum quantity (123)
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = 0;
        var values = TestHelper.CreateTestUshortArray(123, 500);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteMultipleRegisters);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteMultipleRegistersAsync(slaveId, startAddress, values);

        // Assert
        VerifyWriteRequest(slaveId, ModbusFunction.WriteMultipleRegisters, startAddress, 123);
    }

    [Fact]
    public async Task WriteMultipleRegistersAsync_SpecialValues_Success()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = 0;
        var values = new ushort[] { 0, 32767, 65535 }; // Min, mid, max values
        
        var response = new ModbusResponse(slaveId, ModbusFunction.WriteMultipleRegisters);
        SetupSuccessfulRequest(response);

        // Act
        await _client.WriteMultipleRegistersAsync(slaveId, startAddress, values);

        // Assert
        VerifyWriteRequest(slaveId, ModbusFunction.WriteMultipleRegisters, startAddress, 3);
    }

    [Fact]
    public async Task WriteMultipleRegistersAsync_EmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var emptyArray = new ushort[0];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.WriteMultipleRegistersAsync(TestHelper.TestSlaveId, 0, emptyArray));
    }

    [Fact]
    public async Task WriteMultipleRegistersAsync_NullArray_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.WriteMultipleRegistersAsync(TestHelper.TestSlaveId, 0, null!));
    }

    [Fact]
    public async Task WriteMultipleRegistersAsync_TooManyRegisters_ThrowsArgumentException()
    {
        // Arrange
        var tooManyRegisters = TestHelper.CreateTestUshortArray(124, 100); // Exceeds limit of 123

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.WriteMultipleRegistersAsync(TestHelper.TestSlaveId, 0, tooManyRegisters));
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulRequest(ModbusResponse response)
    {
        _mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>()))
            .Returns(new byte[] { 0x01, 0x05, 0x00, 0x64, 0xFF, 0x00 });
        
        _mockTransport.Setup(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x05, 0x00, 0x64, 0xFF, 0x00 });
        
        _mockProtocol.Setup(p => p.ValidateResponse(It.IsAny<byte[]>()))
            .Returns(true);
        
        _mockProtocol.Setup(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()))
            .Returns(response);
    }

    private void VerifyWriteRequest(byte slaveId, ModbusFunction function, ushort startAddress, ushort quantity)
    {
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r => 
            r.SlaveId == slaveId &&
            r.Function == function &&
            r.StartAddress == startAddress &&
            r.Quantity == quantity)), Times.Once);
        
        _mockTransport.Verify(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    /// <summary>
    /// 可测试的Modbus客户端实现
    /// </summary>
    private class TestableModbusClient : ModbusClientBase
    {
        public TestableModbusClient(IModbusTransport transport, IModbusProtocol protocol) 
            : base(transport, protocol)
        {
        }
    }
}