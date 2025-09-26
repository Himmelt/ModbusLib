using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using Moq;

namespace ModbusLib.Tests.Clients;

public class ModbusClientBaseTests
{
    private Mock<IModbusTransport> _mockTransport;
    private Mock<IModbusProtocol> _mockProtocol;
    private TestableModbusClient _client;

    public class TestableModbusClient : ModbusClientBase
    {
        public TestableModbusClient(IModbusTransport transport, IModbusProtocol protocol) 
            : base(transport, protocol)
        {
        }

        // Expose protected members for testing
        public IModbusTransport GetTransport() => Transport;
        public IModbusProtocol GetProtocol() => Protocol;
    }

    public class TestableModbusClientWithProtectedMembers : ModbusClientBase
    {
        public TestableModbusClientWithProtectedMembers(IModbusTransport transport, IModbusProtocol protocol)
            : base(transport, protocol)
        {
        }

        public async Task<ModbusResponse> PublicExecuteRequestAsync(ModbusRequest request, CancellationToken cancellationToken)
        {
            return await ExecuteRequestAsync(request, cancellationToken);
        }
    }

    public ModbusClientBaseTests()
    {
        _mockTransport = new Mock<IModbusTransport>();
        _mockProtocol = new Mock<IModbusProtocol>();
        _client = new TestableModbusClient(_mockTransport.Object, _mockProtocol.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesClient()
    {
        // Assert
        Assert.NotNull(_client);
        Assert.NotNull(_client.GetTransport());
        Assert.NotNull(_client.GetProtocol());
    }

    [Fact]
    public void Constructor_WithNullTransport_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableModbusClient(null!, _mockProtocol.Object));
    }

    [Fact]
    public void Constructor_WithNullProtocol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableModbusClient(_mockTransport.Object, null!));
    }

    [Fact]
    public async Task ConnectAsync_WhenTransportConnects_ReturnsTrue()
    {
        // Arrange
        _mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _client.ConnectAsync();

        // Assert
        Assert.True(result);
        _mockTransport.Verify(t => t.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_CallsTransportDisconnect()
    {
        // Arrange
        _mockTransport.Setup(t => t.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _client.DisconnectAsync();

        // Assert
        _mockTransport.Verify(t => t.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IsConnected_ReturnsTransportIsConnected()
    {
        // Arrange
        _mockTransport.Setup(t => t.IsConnected).Returns(true);

        // Act
        var result = _client.IsConnected;

        // Assert
        Assert.True(result);
        _mockTransport.VerifyGet(t => t.IsConnected, Times.Once);
    }

    [Fact]
    public async Task ExecuteRequestAsync_WhenNotConnected_ThrowsModbusConnectionException()
    {
        // Arrange
        _mockTransport.Setup(t => t.IsConnected).Returns(false);
        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 1);
        var clientWithProtected = new TestableModbusClientWithProtectedMembers(_mockTransport.Object, _mockProtocol.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ModbusConnectionException>(() => 
            clientWithProtected.PublicExecuteRequestAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteRequestAsync_WhenTransportThrowsException_RetriesAndThrows()
    {
        // Arrange
        _mockTransport.Setup(t => t.IsConnected).Returns(true);
        _mockProtocol.Setup(p => p.BuildRequest(It.IsAny<ModbusRequest>())).Returns(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 });
        _mockTransport.Setup(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ModbusCommunicationException("Test exception"));

        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 1);
        var clientWithProtected = new TestableModbusClientWithProtectedMembers(_mockTransport.Object, _mockProtocol.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ModbusCommunicationException>(() =>
            clientWithProtected.PublicExecuteRequestAsync(request, CancellationToken.None));

        // Verify retry attempts (1 initial + 3 retries = 4 total)
        _mockTransport.Verify(t => t.SendReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public void Timeout_Property_GetAndSetWorks()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        _mockTransport.SetupProperty(t => t.Timeout, TimeSpan.FromSeconds(10));

        // Act
        _client.Timeout = timeout;
        var result = _client.Timeout;

        // Assert
        Assert.Equal(timeout, result);
    }

    [Fact]
    public void Retries_Property_GetAndSetWorks()
    {
        // Arrange
        var retries = 5;

        // Act
        _client.Retries = retries;
        var result = _client.Retries;

        // Assert
        Assert.Equal(retries, result);
    }
}