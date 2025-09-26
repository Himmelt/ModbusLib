using ModbusLib.Clients;
using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using Moq;

namespace ModbusLib.Tests.Clients;

public class ModbusTcpClientTests
{
    [Fact]
    public void Constructor_WithNetworkConfig_CreatesClient()
    {
        // Arrange
        var config = new NetworkConnectionConfig
        {
            Host = "127.0.0.1",
            Port = 502
        };

        // Act
        var client = new ModbusTcpClient(config);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithTransport_CreatesClient()
    {
        // Arrange
        var mockTransport = new Mock<TcpTransport>(new NetworkConnectionConfig { Host = "127.0.0.1", Port = 502 });
        var transport = mockTransport.Object;

        // Act
        var client = new ModbusTcpClient(transport);

        // Assert
        Assert.NotNull(client);
    }
}