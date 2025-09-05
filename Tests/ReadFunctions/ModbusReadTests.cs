using FluentAssertions;
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using Moq;
using Xunit;

namespace ModbusLib.Tests.ReadFunctions;

/// <summary>
/// Modbus 读取功能测试
/// </summary>
public class ModbusReadTests
{
    private readonly Mock<IModbusTransport> _mockTransport;
    private readonly Mock<IModbusProtocol> _mockProtocol;
    private readonly TestableModbusClient _client;

    public ModbusReadTests()
    {
        _mockTransport = new Mock<IModbusTransport>();
        _mockProtocol = new Mock<IModbusProtocol>();
        _client = new TestableModbusClient(_mockTransport.Object, _mockProtocol.Object);
        
        // 设置默认的连接状态
        _mockTransport.Setup(t => t.IsConnected).Returns(true);
        _mockTransport.Setup(t => t.Timeout).Returns(TestHelper.DefaultTimeout);
    }

    #region ReadCoils Tests

    [Fact]
    public async Task ReadCoilsAsync_ValidRequest_ReturnsCorrectBoolArray()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 8;
        
        var expectedCoils = TestHelper.CreateTestBoolArray(8, true);
        var responseData = new byte[] { 1, 0xFF }; // 8 coils all true
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadCoils, responseData);
        
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadCoilsAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(8);
        result.Should().AllSatisfy(coil => coil.Should().BeTrue());
        
        VerifyRequestWasSent(slaveId, ModbusFunction.ReadCoils, startAddress, quantity);
    }

    [Fact]
    public async Task ReadCoilsAsync_BoundaryValues_Success()
    {
        // Arrange - Test minimum quantity
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 1;
        
        var responseData = new byte[] { 1, 0x01 }; // 1 coil = true
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadCoils, responseData);
        
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadCoilsAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeTrue();
    }

    [Fact]
    public async Task ReadCoilsAsync_MaximumQuantity_Success()
    {
        // Arrange - Test maximum quantity (2000)
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 2000;
        
        var byteCount = (quantity + 7) / 8; // 250 bytes
        var responseData = new byte[byteCount + 1];
        responseData[0] = (byte)byteCount;
        // Fill with alternating pattern
        for (int i = 1; i <= byteCount; i++)
        {
            responseData[i] = (byte)(i % 2 == 1 ? 0xAA : 0x55);
        }
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadCoils, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadCoilsAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().HaveCount(2000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2001)]
    public async Task ReadCoilsAsync_InvalidQuantity_ThrowsArgumentException(ushort invalidQuantity)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.ReadCoilsAsync(TestHelper.TestSlaveId, TestHelper.TestStartAddress, invalidQuantity));
    }

    [Fact]
    public async Task ReadCoilsAsync_ErrorResponse_ThrowsModbusException()
    {
        // Arrange
        var errorResponse = ModbusResponse.CreateError(TestHelper.TestSlaveId, 
            ModbusFunction.ReadCoils, ModbusExceptionCode.IllegalDataAddress);
        
        SetupSuccessfulRequest(errorResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModbusException>(() => 
            _client.ReadCoilsAsync(TestHelper.TestSlaveId, TestHelper.TestStartAddress, 8));
        
        exception.ExceptionCode.Should().Be(ModbusExceptionCode.IllegalDataAddress);
        exception.Function.Should().Be(ModbusFunction.ReadCoils);
    }

    #endregion

    #region ReadDiscreteInputs Tests

    [Fact]
    public async Task ReadDiscreteInputsAsync_ValidRequest_ReturnsCorrectBoolArray()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 16;
        
        var responseData = new byte[] { 2, 0xAA, 0x55 }; // Alternating pattern
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadDiscreteInputs, responseData);
        
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadDiscreteInputsAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(16);
        
        // Verify alternating pattern
        for (int i = 0; i < 8; i++)
        {
            result[i].Should().Be(i % 2 == 1); // 0xAA pattern
        }
        for (int i = 8; i < 16; i++)
        {
            result[i].Should().Be(i % 2 == 0); // 0x55 pattern
        }
        
        VerifyRequestWasSent(slaveId, ModbusFunction.ReadDiscreteInputs, startAddress, quantity);
    }

    [Fact]
    public async Task ReadDiscreteInputsAsync_InsufficientResponseData_ThrowsCommunicationException()
    {
        // Arrange
        var responseData = new byte[] { 2 }; // Missing data bytes
        var response = new ModbusResponse(TestHelper.TestSlaveId, ModbusFunction.ReadDiscreteInputs, responseData);
        
        SetupSuccessfulRequest(response);

        // Act & Assert
        await Assert.ThrowsAsync<ModbusCommunicationException>(() => 
            _client.ReadDiscreteInputsAsync(TestHelper.TestSlaveId, TestHelper.TestStartAddress, 16));
    }

    #endregion

    #region ReadHoldingRegisters Tests

    [Fact]
    public async Task ReadHoldingRegistersAsync_ValidRequest_ReturnsCorrectUshortArray()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 10;
        
        var expectedValues = TestHelper.CreateTestUshortArray(10, 1000);
        var dataBytes = ModbusUtils.UshortArrayToByteArray(expectedValues);
        var responseData = new byte[1 + dataBytes.Length];
        responseData[0] = (byte)dataBytes.Length;
        Array.Copy(dataBytes, 0, responseData, 1, dataBytes.Length);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadHoldingRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadHoldingRegistersAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
        result.Should().BeEquivalentTo(expectedValues);
        
        VerifyRequestWasSent(slaveId, ModbusFunction.ReadHoldingRegisters, startAddress, quantity);
    }

    [Fact]
    public async Task ReadHoldingRegistersAsync_MaximumQuantity_Success()
    {
        // Arrange - Test maximum quantity (125)
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 125;
        
        var expectedValues = TestHelper.CreateTestUshortArray(125, 500);
        var dataBytes = ModbusUtils.UshortArrayToByteArray(expectedValues);
        var responseData = new byte[1 + dataBytes.Length];
        responseData[0] = (byte)dataBytes.Length;
        Array.Copy(dataBytes, 0, responseData, 1, dataBytes.Length);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadHoldingRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadHoldingRegistersAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().HaveCount(125);
        result.Should().BeEquivalentTo(expectedValues);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(126)]
    public async Task ReadHoldingRegistersAsync_InvalidQuantity_ThrowsArgumentException(ushort invalidQuantity)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.ReadHoldingRegistersAsync(TestHelper.TestSlaveId, TestHelper.TestStartAddress, invalidQuantity));
    }

    [Fact]
    public async Task ReadHoldingRegistersAsync_IncorrectDataLength_ThrowsCommunicationException()
    {
        // Arrange - Response has wrong data length
        var responseData = new byte[] { 10, 0x01, 0x00 }; // Says 10 bytes but only has 2
        var response = new ModbusResponse(TestHelper.TestSlaveId, ModbusFunction.ReadHoldingRegisters, responseData);
        
        SetupSuccessfulRequest(response);

        // Act & Assert
        await Assert.ThrowsAsync<ModbusCommunicationException>(() => 
            _client.ReadHoldingRegistersAsync(TestHelper.TestSlaveId, TestHelper.TestStartAddress, 5));
    }

    #endregion

    #region ReadInputRegisters Tests

    [Fact]
    public async Task ReadInputRegistersAsync_ValidRequest_ReturnsCorrectUshortArray()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 5;
        
        var expectedValues = new ushort[] { 100, 200, 300, 400, 500 };
        var dataBytes = ModbusUtils.UshortArrayToByteArray(expectedValues);
        var responseData = new byte[1 + dataBytes.Length];
        responseData[0] = (byte)dataBytes.Length;
        Array.Copy(dataBytes, 0, responseData, 1, dataBytes.Length);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadInputRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadInputRegistersAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Should().BeEquivalentTo(expectedValues);
        
        VerifyRequestWasSent(slaveId, ModbusFunction.ReadInputRegisters, startAddress, quantity);
    }

    [Fact]
    public async Task ReadInputRegistersAsync_SpecialValues_Success()
    {
        // Arrange - Test special values (0, max ushort)
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort startAddress = TestHelper.TestStartAddress;
        const ushort quantity = 3;
        
        var expectedValues = new ushort[] { 0, 32767, 65535 };
        var dataBytes = ModbusUtils.UshortArrayToByteArray(expectedValues);
        var responseData = new byte[1 + dataBytes.Length];
        responseData[0] = (byte)dataBytes.Length;
        Array.Copy(dataBytes, 0, responseData, 1, dataBytes.Length);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadInputRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadInputRegistersAsync(slaveId, startAddress, quantity);

        // Assert
        result.Should().BeEquivalentTo(expectedValues);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulRequest(ModbusResponse response)
    {
        _mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>()))
            .Returns(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 });
        
        _mockTransport.Setup(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x03, 0x02, 0x01, 0x00 });
        
        _mockProtocol.Setup(p => p.ValidateResponse(It.IsAny<byte[]>()))
            .Returns(true);
        
        _mockProtocol.Setup(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()))
            .Returns(response);
    }

    private void VerifyRequestWasSent(byte slaveId, ModbusFunction function, ushort startAddress, ushort quantity)
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