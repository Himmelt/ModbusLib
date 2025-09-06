using FluentAssertions;
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using Moq;
using Xunit;

namespace ModbusLib.Tests.AdvancedFunctions;

/// <summary>
/// Modbus 复合读写功能测试
/// </summary>
public class ModbusAdvancedTests
{
    private readonly Mock<IModbusTransport> _mockTransport;
    private readonly Mock<IModbusProtocol> _mockProtocol;
    private readonly TestableModbusClient _client;

    public ModbusAdvancedTests()
    {
        _mockTransport = new Mock<IModbusTransport>();
        _mockProtocol = new Mock<IModbusProtocol>();
        _client = new TestableModbusClient(_mockTransport.Object, _mockProtocol.Object);
        
        // 设置默认的连接状态
        _mockTransport.Setup(t => t.IsConnected).Returns(true);
        _mockTransport.Setup(t => t.Timeout).Returns(TestHelper.DefaultTimeout);
    }

    #region ReadWriteMultipleRegisters Tests

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_ValidRequest_ReturnsCorrectData()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort readStartAddress = 0;
        const ushort readQuantity = 10;
        const ushort writeStartAddress = 100;
        var writeValues = TestHelper.CreateTestUshortArray(5, 1000);
        
        var expectedReadValues = TestHelper.CreateTestUshortArray(10, 500);
        var readDataBytes = ModbusUtils.UshortArrayToByteArray(expectedReadValues);
        var responseData = new byte[1 + readDataBytes.Length];
        responseData[0] = (byte)readDataBytes.Length;
        Array.Copy(readDataBytes, 0, responseData, 1, readDataBytes.Length);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadWriteMultipleRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadWriteMultipleRegistersAsync(
            slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
        result.Should().BeEquivalentTo(expectedReadValues);
        
        VerifyReadWriteRequest(slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_RequestDataFormat_CorrectStructure()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort readStartAddress = 0x10;
        const ushort readQuantity = 5;
        const ushort writeStartAddress = 0x20;
        var writeValues = new ushort[] { 0x1234, 0x5678, 0x9ABC };
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadWriteMultipleRegisters, 
            new byte[] { 10, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05 });
        SetupSuccessfulRequest(response);

        // Act
        await _client.ReadWriteMultipleRegistersAsync(
            slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);

        // Assert - Verify the request data format
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.SlaveId == slaveId &&
            r.Function == ModbusFunction.ReadWriteMultipleRegisters &&
            r.StartAddress == readStartAddress &&
            r.Quantity == readQuantity &&
            r.Data != null &&
            r.Data.Length == 4 + (writeValues.Length * 2) && // 4 bytes header + write data
            // Verify write start address (big-endian)
            r.Data[0] == 0x00 && r.Data[1] == 0x20 &&
            // Verify write quantity (big-endian)
            r.Data[2] == 0x00 && r.Data[3] == 0x03)), Times.Once);
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_WriteDataIntegrity_CorrectByteOrder()
    {
        // Arrange
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort readStartAddress = 0;
        const ushort readQuantity = 1;
        const ushort writeStartAddress = 100;
        var writeValues = new ushort[] { 0x1234 }; // Test specific value for byte order
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadWriteMultipleRegisters, 
            new byte[] { 2, 0x00, 0x01 });
        SetupSuccessfulRequest(response);

        // Act
        await _client.ReadWriteMultipleRegistersAsync(
            slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);

        // Assert - Verify write data is in correct byte order (big-endian)
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.Data != null &&
            r.Data.Length >= 6 &&
            r.Data[4] == 0x12 && // High byte first
            r.Data[5] == 0x34)), Times.Once);
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_MaximumReadQuantity_Success()
    {
        // Arrange - Test maximum read quantity (125)
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort readStartAddress = 0;
        const ushort readQuantity = 125;
        const ushort writeStartAddress = 200;
        var writeValues = TestHelper.CreateTestUshortArray(5, 1000);
        
        var expectedReadValues = TestHelper.CreateTestUshortArray(125, 500);
        var readDataBytes = ModbusUtils.UshortArrayToByteArray(expectedReadValues);
        var responseData = new byte[1 + readDataBytes.Length];
        responseData[0] = (byte)readDataBytes.Length;
        Array.Copy(readDataBytes, 0, responseData, 1, readDataBytes.Length);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadWriteMultipleRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadWriteMultipleRegistersAsync(
            slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);

        // Assert
        result.Should().HaveCount(125);
        result.Should().BeEquivalentTo(expectedReadValues);
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_MaximumWriteQuantity_Success()
    {
        // Arrange - Test maximum write quantity (121)
        const byte slaveId = TestHelper.TestSlaveId;
        const ushort readStartAddress = 0;
        const ushort readQuantity = 5;
        const ushort writeStartAddress = 100;
        var writeValues = TestHelper.CreateTestUshortArray(121, 1000);
        
        var responseData = new byte[] { 10, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05 };
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadWriteMultipleRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadWriteMultipleRegistersAsync(
            slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);

        // Assert
        result.Should().HaveCount(5);
        VerifyReadWriteRequest(slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(126)]
    public async Task ReadWriteMultipleRegistersAsync_InvalidReadQuantity_ThrowsArgumentException(ushort invalidReadQuantity)
    {
        // Arrange
        var writeValues = TestHelper.CreateTestUshortArray(5, 1000);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.ReadWriteMultipleRegistersAsync(
                TestHelper.TestSlaveId, 0, invalidReadQuantity, 100, writeValues));
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_EmptyWriteArray_ThrowsArgumentException()
    {
        // Arrange
        var emptyArray = new ushort[0];

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.ReadWriteMultipleRegistersAsync(
                TestHelper.TestSlaveId, 0, 5, 100, emptyArray));
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_NullWriteArray_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.ReadWriteMultipleRegistersAsync(
                TestHelper.TestSlaveId, 0, 5, 100, null!));
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_TooManyWriteRegisters_ThrowsArgumentException()
    {
        // Arrange
        var tooManyRegisters = TestHelper.CreateTestUshortArray(122, 100); // Exceeds limit of 121

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _client.ReadWriteMultipleRegistersAsync(
                TestHelper.TestSlaveId, 0, 5, 100, tooManyRegisters));
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_IncorrectResponseDataLength_ThrowsCommunicationException()
    {
        // Arrange
        var writeValues = TestHelper.CreateTestUshortArray(3, 1000);
        var responseData = new byte[] { 10, 0x01, 0x00 }; // Says 10 bytes but only has 2
        var response = new ModbusResponse(TestHelper.TestSlaveId, ModbusFunction.ReadWriteMultipleRegisters, responseData);
        
        SetupSuccessfulRequest(response);

        // Act & Assert
        await Assert.ThrowsAsync<ModbusCommunicationException>(() => 
            _client.ReadWriteMultipleRegistersAsync(
                TestHelper.TestSlaveId, 0, 5, 100, writeValues));
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_ErrorResponse_ThrowsModbusException()
    {
        // Arrange
        var writeValues = TestHelper.CreateTestUshortArray(3, 1000);
        var errorResponse = ModbusResponse.CreateError(TestHelper.TestSlaveId, 
            ModbusFunction.ReadWriteMultipleRegisters, ModbusExceptionCode.IllegalDataAddress);
        
        SetupSuccessfulRequest(errorResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ModbusException>(() => 
            _client.ReadWriteMultipleRegistersAsync(
                TestHelper.TestSlaveId, 0, 5, 100, writeValues));
        
        exception.ExceptionCode.Should().Be(ModbusExceptionCode.IllegalDataAddress);
        exception.Function.Should().Be(ModbusFunction.ReadWriteMultipleRegisters);
    }

    [Fact]
    public async Task ReadWriteMultipleRegistersAsync_ComplexScenario_DataIntegrity()
    {
        // Arrange - Complex scenario with different read/write addresses and quantities
        const byte slaveId = 5;
        const ushort readStartAddress = 1000;
        const ushort readQuantity = 20;
        const ushort writeStartAddress = 2000;
        var writeValues = new ushort[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };
        
        var expectedReadValues = new ushort[20];
        for (int i = 0; i < 20; i++)
        {
            expectedReadValues[i] = (ushort)(i * 10 + 50);
        }
        
        var readDataBytes = ModbusUtils.UshortArrayToByteArray(expectedReadValues);
        var responseData = new byte[1 + readDataBytes.Length];
        responseData[0] = (byte)readDataBytes.Length;
        Array.Copy(readDataBytes, 0, responseData, 1, readDataBytes.Length);
        
        var response = new ModbusResponse(slaveId, ModbusFunction.ReadWriteMultipleRegisters, responseData);
        SetupSuccessfulRequest(response);

        // Act
        var result = await _client.ReadWriteMultipleRegistersAsync(
            slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues);

        // Assert
        result.Should().HaveCount(20);
        result.Should().BeEquivalentTo(expectedReadValues);
        
        // Verify complete request structure
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r =>
            r.SlaveId == slaveId &&
            r.Function == ModbusFunction.ReadWriteMultipleRegisters &&
            r.StartAddress == readStartAddress &&
            r.Quantity == readQuantity &&
            r.Data != null &&
            r.Data.Length == 4 + (writeValues.Length * 2))), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulRequest(ModbusResponse response)
    {
        _mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>()))
            .Returns(new byte[] { 0x01, 0x17, 0x00, 0x00, 0x00, 0x05, 0x00, 0x64, 0x00, 0x03 });
        
        _mockTransport.Setup(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x17, 0x0A, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05 });
        
        _mockProtocol.Setup(p => p.ValidateResponse(It.IsAny<byte[]>()))
            .Returns(true);
        
        _mockProtocol.Setup(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()))
            .Returns(response);
    }

    private void VerifyReadWriteRequest(byte slaveId, ushort readStartAddress, ushort readQuantity, 
        ushort writeStartAddress, ushort[] writeValues)
    {
        _mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r => 
            r.SlaveId == slaveId &&
            r.Function == ModbusFunction.ReadWriteMultipleRegisters &&
            r.StartAddress == readStartAddress &&
            r.Quantity == readQuantity &&
            r.Data != null &&
            r.Data.Length == 4 + (writeValues.Length * 2))), Times.Once);
        
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