using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Transports;
using Xunit;

namespace ModbusLib.Tests.Transports;

public class UdpTransportTests {
    [Fact]
    public void Constructor_WithValidConfig_ShouldInitializeProperties() {
        // Arrange
        var config = new NetworkConnectionConfig {
            Host = "127.0.0.1",
            Port = 502
        };

        // Act
        var transport = new UdpTransport(config);

        // Assert
        Assert.NotNull(transport);
        Assert.False(transport.IsConnected);
        Assert.IsAssignableFrom<IModbusTransport>(transport);
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UdpTransport(null!));
    }

    [Fact]
    public void IsConnected_WhenNotConnected_ShouldReturnFalse() {
        // Arrange
        var config = new NetworkConnectionConfig();
        var transport = new UdpTransport(config);

        // Act
        var isConnected = transport.IsConnected;

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void Timeout_Property_ShouldGetAndSetCorrectly() {
        // Arrange
        var config = new NetworkConnectionConfig();
        var transport = new UdpTransport(config);
        var expectedTimeout = TimeSpan.FromSeconds(10);

        // Act
        transport.Timeout = expectedTimeout;
        var actualTimeout = transport.Timeout;

        // Assert
        Assert.Equal(expectedTimeout, actualTimeout);
    }
}