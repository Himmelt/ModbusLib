using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Factories;
using ModbusLib.Models;

namespace ModbusLib.Tests.Factories;

public class ModbusClientFactoryTests
{
    [Fact]
    public void CreateRtuClient_WithConfig_ReturnsModbusRtuClient()
    {
        // Arrange
        var config = new SerialConnectionConfig
        {
            PortName = "COM1",
            BaudRate = 9600
        };

        // Act
        var client = ModbusClientFactory.CreateRtuClient(config);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<ModbusRtuClient>(client);
    }

    [Fact]
    public void CreateRtuClient_WithPortNameAndBaudRate_ReturnsModbusRtuClient()
    {
        // Arrange
        var portName = "COM1";
        var baudRate = 9600;

        // Act
        var client = ModbusClientFactory.CreateRtuClient(portName, baudRate);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<ModbusRtuClient>(client);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateRtuClient_WithInvalidPortName_ThrowsArgumentException(string? portName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ModbusClientFactory.CreateRtuClient(portName!, 9600));
    }

    [Fact]
    public void CreateTcpClient_WithConfig_ReturnsModbusTcpClient()
    {
        // Arrange
        var config = new NetworkConnectionConfig
        {
            Host = "127.0.0.1",
            Port = 502
        };

        // Act
        var client = ModbusClientFactory.CreateTcpClient(config);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<ModbusTcpClient>(client);
    }

    [Fact]
    public void CreateTcpClient_WithHostAndPort_ReturnsModbusTcpClient()
    {
        // Arrange
        var host = "127.0.0.1";
        var port = 502;

        // Act
        var client = ModbusClientFactory.CreateTcpClient(host, port);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<ModbusTcpClient>(client);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateTcpClient_WithInvalidHost_ThrowsArgumentException(string? host)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ModbusClientFactory.CreateTcpClient(host!, 502));
    }
}