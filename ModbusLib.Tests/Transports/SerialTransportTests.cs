using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Transports;
using Xunit;

namespace ModbusLib.Tests.Transports;

public class SerialTransportTests
{
    [Fact]
    public void Constructor_WithValidConfig_ShouldInitializeProperties()
    {
        // Arrange
        var config = new SerialConnectionConfig
        {
            PortName = "COM1",
            BaudRate = 9600
        };

        // Act
        var transport = new SerialTransport(config);

        // Assert
        Assert.NotNull(transport);
        Assert.False(transport.IsConnected);
        Assert.IsAssignableFrom<IModbusTransport>(transport);
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SerialTransport(null!));
    }

    [Fact]
    public void IsConnected_WhenNotConnected_ShouldReturnFalse()
    {
        // Arrange
        var config = new SerialConnectionConfig
        {
            PortName = "COM1",
            BaudRate = 9600
        };
        var transport = new SerialTransport(config);

        // Act
        var isConnected = transport.IsConnected;

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void Timeout_Property_ShouldGetAndSetCorrectly()
    {
        // Arrange
        var config = new SerialConnectionConfig
        {
            PortName = "COM1",
            BaudRate = 9600
        };
        var transport = new SerialTransport(config);
        var expectedTimeout = TimeSpan.FromSeconds(10);

        // Act
        transport.Timeout = expectedTimeout;
        var actualTimeout = transport.Timeout;

        // Assert
        Assert.Equal(expectedTimeout, actualTimeout);
    }
}