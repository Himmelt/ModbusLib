using FluentAssertions;
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Factories;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using Moq;
using Xunit;

namespace ModbusLib.Tests.Integration;

/// <summary>
/// 基础客户端集成测试
/// </summary>
public class BasicClientIntegrationTests
{
    #region Connection Management Tests

    [Fact]
    public void ModbusClient_InitialState_IsNotConnected()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        mockTransport.Setup(t => t.IsConnected).Returns(false);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act & Assert
        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ModbusClient_ConnectAsync_UpdatesConnectionState()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        var result = await client.ConnectAsync();

        // Assert
        result.Should().BeTrue();
        client.IsConnected.Should().BeTrue();
        mockTransport.Verify(t => t.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ModbusClient_DisconnectAsync_UpdatesConnectionState()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        mockTransport.Setup(t => t.IsConnected).Returns(false);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        await client.DisconnectAsync();

        // Assert
        client.IsConnected.Should().BeFalse();
        mockTransport.Verify(t => t.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ModbusClient_ConnectFailed_ReturnsFalse()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        mockTransport.Setup(t => t.IsConnected).Returns(false);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        var result = await client.ConnectAsync();

        // Assert
        result.Should().BeFalse();
        client.IsConnected.Should().BeFalse();
    }

    #endregion

    #region Timeout and Retry Configuration Tests

    [Fact]
    public void ModbusClient_TimeoutProperty_SetsTransportTimeout()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        var expectedTimeout = TimeSpan.FromSeconds(10);
        
        mockTransport.SetupProperty(t => t.Timeout);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        client.Timeout = expectedTimeout;

        // Assert
        mockTransport.VerifySet(t => t.Timeout = expectedTimeout, Times.Once);
    }

    [Fact]
    public void ModbusClient_RetriesProperty_DefaultValue()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act & Assert
        client.Retries.Should().Be(3); // Default retry count
    }

    [Fact]
    public void ModbusClient_RetriesProperty_CanBeSet()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        client.Retries = 5;

        // Assert
        client.Retries.Should().Be(5);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ModbusClient_RequestWhenNotConnected_ThrowsConnectionException()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        mockTransport.Setup(t => t.IsConnected).Returns(false);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ModbusConnectionException>(() => 
            client.ReadCoilsAsync(1, 0, 8));
    }

    [Fact]
    public async Task ModbusClient_TransportException_PerformsRetry()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.Timeout).Returns(TimeSpan.FromSeconds(5));
        
        // Setup to fail first two attempts, succeed on third
        mockTransport.SetupSequence(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ModbusCommunicationException("First failure"))
            .ThrowsAsync(new ModbusCommunicationException("Second failure"))
            .ReturnsAsync(new byte[] { 0x01, 0x01, 0x01, 0xFF });

        mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>()))
            .Returns(new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x08 });
        
        mockProtocol.Setup(p => p.ValidateResponse(It.IsAny<byte[]>())).Returns(true);
        
        var response = new ModbusResponse(1, ModbusFunction.ReadCoils, new byte[] { 1, 0xFF });
        mockProtocol.Setup(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()))
            .Returns(response);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        var result = await client.ReadCoilsAsync(1, 0, 8);

        // Assert
        result.Should().NotBeNull();
        mockTransport.Verify(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ModbusClient_ExceedsMaxRetries_ThrowsException()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.Timeout).Returns(TimeSpan.FromSeconds(5));
        
        // Always fail
        mockTransport.Setup(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ModbusCommunicationException("Communication failed"));

        mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>()))
            .Returns(new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x08 });
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);
        client.Retries = 2; // Set low retry count for faster test

        // Act & Assert
        await Assert.ThrowsAsync<ModbusCommunicationException>(() => 
            client.ReadCoilsAsync(1, 0, 8));
        
        // Should try initial + 2 retries = 3 total attempts
        mockTransport.Verify(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ModbusClient_NonRetryableException_DoesNotRetry()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.Timeout).Returns(TimeSpan.FromSeconds(5));
        
        // Throw non-retryable exception
        mockTransport.Setup(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid argument"));

        mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>()))
            .Returns(new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x08 });
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            client.ReadCoilsAsync(1, 0, 8));
        
        // Should only try once (no retries for non-retryable exceptions)
        mockTransport.Verify(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Simple Read/Write Operation Tests

    [Fact]
    public async Task ModbusClient_SimpleReadOperation_Success()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        SetupConnectedClient(mockTransport, mockProtocol);
        
        var expectedData = new byte[] { 1, 0xFF }; // 8 coils, all true
        var response = new ModbusResponse(1, ModbusFunction.ReadCoils, expectedData);
        
        mockProtocol.Setup(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()))
            .Returns(response);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        var result = await client.ReadCoilsAsync(1, 0, 8);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(8);
        result.Should().AllSatisfy(coil => coil.Should().BeTrue());
        
        VerifyBasicOperation(mockTransport, mockProtocol, ModbusFunction.ReadCoils);
    }

    [Fact]
    public async Task ModbusClient_SimpleWriteOperation_Success()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        SetupConnectedClient(mockTransport, mockProtocol);
        
        var response = new ModbusResponse(1, ModbusFunction.WriteSingleCoil);
        
        mockProtocol.Setup(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()))
            .Returns(response);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        await client.WriteSingleCoilAsync(1, 50, true);

        // Assert - No exception thrown means success
        VerifyBasicOperation(mockTransport, mockProtocol, ModbusFunction.WriteSingleCoil);
    }

    [Fact]
    public async Task ModbusClient_ReadWriteSequence_Success()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        SetupConnectedClient(mockTransport, mockProtocol);
        
        // Setup responses for different operations
        var readResponse = new ModbusResponse(1, ModbusFunction.ReadHoldingRegisters, new byte[] { 2, 0x01, 0x00 });
        var writeResponse = new ModbusResponse(1, ModbusFunction.WriteSingleRegister);
        
        mockProtocol.SetupSequence(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()))
            .Returns(readResponse)
            .Returns(writeResponse);
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        var readResult = await client.ReadHoldingRegistersAsync(1, 0, 1);
        await client.WriteSingleRegisterAsync(1, 0, 200);

        // Assert
        readResult.Should().NotBeNull();
        readResult.Should().HaveCount(1);
        readResult[0].Should().Be(256); // 0x0100 in big-endian
        
        // Verify both operations were called
        mockTransport.Verify(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void ModbusClient_Dispose_DisposesTransport()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);

        // Act
        client.Dispose();

        // Assert
        mockTransport.Verify(t => t.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ModbusClient_OperationAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);
        client.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => 
            client.ReadCoilsAsync(1, 0, 8));
    }

    [Fact]
    public async Task ModbusClient_ConnectAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);
        client.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => 
            client.ConnectAsync());
    }

    [Fact]
    public async Task ModbusClient_DisconnectAfterDispose_DoesNotThrow()
    {
        // Arrange
        var mockTransport = new Mock<IModbusTransport>();
        var mockProtocol = new Mock<IModbusProtocol>();
        
        var client = new TestableModbusClient(mockTransport.Object, mockProtocol.Object);
        client.Dispose();

        // Act & Assert - Should not throw
        await client.DisconnectAsync();
    }

    #endregion

    #region Factory Integration Tests

    [Fact]
    public void FactoryCreateClients_AllTypes_ImplementsIModbusClient()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "127.0.0.1", Port = 502 };
        var serialConfig = new SerialConnectionConfig { PortName = "COM1", BaudRate = 9600 };

        // Act & Assert
        var tcpClient = ModbusClientFactory.CreateTcpClient(networkConfig);
        tcpClient.Should().BeAssignableTo<IModbusClient>();
        tcpClient.Should().NotBeNull();

        var udpClient = ModbusClientFactory.CreateUdpClient(networkConfig);
        udpClient.Should().BeAssignableTo<IModbusClient>();
        udpClient.Should().NotBeNull();

        var rtuClient = ModbusClientFactory.CreateRtuClient(serialConfig);
        rtuClient.Should().BeAssignableTo<IModbusClient>();
        rtuClient.Should().NotBeNull();

        var rtuOverTcpClient = ModbusClientFactory.CreateRtuOverTcpClient(networkConfig);
        rtuOverTcpClient.Should().BeAssignableTo<IModbusClient>();
        rtuOverTcpClient.Should().NotBeNull();

        var rtuOverUdpClient = ModbusClientFactory.CreateRtuOverUdpClient(networkConfig);
        rtuOverUdpClient.Should().BeAssignableTo<IModbusClient>();
        rtuOverUdpClient.Should().NotBeNull();
    }

    [Fact]
    public void FactoryCreateClients_HasCorrectInitialState()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "127.0.0.1", Port = 502 };

        // Act
        var client = ModbusClientFactory.CreateTcpClient(networkConfig);

        // Assert
        client.IsConnected.Should().BeFalse();
        client.Retries.Should().Be(3); // Default retry count
        client.Timeout.Should().Be(TimeSpan.FromSeconds(5)); // Default timeout
    }

    #endregion

    #region Helper Methods

    private static void SetupConnectedClient(Mock<IModbusTransport> mockTransport, Mock<IModbusProtocol> mockProtocol)
    {
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.Timeout).Returns(TimeSpan.FromSeconds(5));
        
        mockTransport.Setup(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x01, 0x01, 0xFF });
        
        mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>()))
            .Returns(new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x08 });
        
        mockProtocol.Setup(p => p.ValidateResponse(It.IsAny<byte[]>())).Returns(true);
    }

    private static void VerifyBasicOperation(Mock<IModbusTransport> mockTransport, Mock<IModbusProtocol> mockProtocol, ModbusFunction expectedFunction)
    {
        mockProtocol.Verify(p => p.BuildRequest(It.Is<ModbusRequest>(r => r.Function == expectedFunction)), Times.Once);
        mockTransport.Verify(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        mockProtocol.Verify(p => p.ValidateResponse(It.IsAny<byte[]>()), Times.Once);
        mockProtocol.Verify(p => p.ParseResponse(It.IsAny<byte[]>(), It.IsAny<ModbusRequest>()), Times.Once);
    }

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

    #endregion
}