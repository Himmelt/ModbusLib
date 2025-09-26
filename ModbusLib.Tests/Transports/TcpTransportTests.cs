using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Transports;
using Xunit;

namespace ModbusLib.Tests.Transports;

public class TcpTransportTests {
    [Fact]
    public void Constructor_WithValidConfig_ShouldInitializeProperties() {
        // Arrange
        var config = new NetworkConnectionConfig {
            Host = "127.0.0.1",
            Port = 502
        };

        // Act
        var transport = new TcpTransport(config);

        // Assert
        Assert.NotNull(transport);
        Assert.False(transport.IsConnected);
        Assert.IsAssignableFrom<IModbusTransport>(transport);
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TcpTransport(null!));
    }

    [Fact]
    public void IsConnected_WhenNotConnected_ShouldReturnFalse() {
        // Arrange
        var config = new NetworkConnectionConfig();
        var transport = new TcpTransport(config);

        // Act
        var isConnected = transport.IsConnected;

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void Timeout_Property_ShouldGetAndSetCorrectly() {
        // Arrange
        var config = new NetworkConnectionConfig();
        var transport = new TcpTransport(config);
        var expectedTimeout = TimeSpan.FromSeconds(10);

        // Act
        transport.Timeout = expectedTimeout;
        var actualTimeout = transport.Timeout;

        // Assert
        Assert.Equal(expectedTimeout, actualTimeout);
    }
}