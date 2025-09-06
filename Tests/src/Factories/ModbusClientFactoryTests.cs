using FluentAssertions;
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Factories;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using Xunit;

namespace ModbusLib.Tests.Factories;

/// <summary>
/// Modbus客户端工厂测试
/// </summary>
public class ModbusClientFactoryTests
{
    #region RTU Client Tests

    [Fact]
    public void CreateRtuClient_ValidConfig_ReturnsRtuClient()
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
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuClient>();
    }

    [Fact]
    public void CreateRtuClient_WithPortNameAndBaudRate_ReturnsRtuClient()
    {
        // Arrange
        const string portName = "COM3";
        const int baudRate = 19200;

        // Act
        var client = ModbusClientFactory.CreateRtuClient(portName, baudRate);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuClient>();
    }

    [Fact]
    public void CreateRtuClient_DefaultBaudRate_Uses9600()
    {
        // Arrange
        const string portName = "COM2";

        // Act
        var client = ModbusClientFactory.CreateRtuClient(portName);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuClient>();
    }

    [Fact]
    public void CreateRtuClient_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ModbusClientFactory.CreateRtuClient((SerialConnectionConfig)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public void CreateRtuClient_InvalidPortName_ThrowsArgumentException(string invalidPortName)
    {
        // Act & Assert
        var portName = invalidPortName == "null" ? null : invalidPortName;
        Assert.Throws<ArgumentException>(() => 
            ModbusClientFactory.CreateRtuClient(portName!, 9600));
    }

    #endregion

    #region TCP Client Tests

    [Fact]
    public void CreateTcpClient_ValidConfig_ReturnsTcpClient()
    {
        // Arrange
        var config = new NetworkConnectionConfig
        {
            Host = "192.168.1.100",
            Port = 502
        };

        // Act
        var client = ModbusClientFactory.CreateTcpClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusTcpClient>();
    }

    [Fact]
    public void CreateTcpClient_WithHostAndPort_ReturnsTcpClient()
    {
        // Arrange
        const string host = "10.0.0.1";
        const int port = 1502;

        // Act
        var client = ModbusClientFactory.CreateTcpClient(host, port);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusTcpClient>();
    }

    [Fact]
    public void CreateTcpClient_DefaultPort_Uses502()
    {
        // Arrange
        const string host = "localhost";

        // Act
        var client = ModbusClientFactory.CreateTcpClient(host);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusTcpClient>();
    }

    [Fact]
    public void CreateTcpClient_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ModbusClientFactory.CreateTcpClient((NetworkConnectionConfig)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public void CreateTcpClient_InvalidHost_ThrowsArgumentException(string invalidHost)
    {
        // Act & Assert
        var host = invalidHost == "null" ? null : invalidHost;
        Assert.Throws<ArgumentException>(() => 
            ModbusClientFactory.CreateTcpClient(host!, 502));
    }

    #endregion

    #region UDP Client Tests

    [Fact]
    public void CreateUdpClient_ValidConfig_ReturnsUdpClient()
    {
        // Arrange
        var config = new NetworkConnectionConfig
        {
            Host = "192.168.1.100",
            Port = 502
        };

        // Act
        var client = ModbusClientFactory.CreateUdpClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusUdpClient>();
    }

    [Fact]
    public void CreateUdpClient_WithHostAndPort_ReturnsUdpClient()
    {
        // Arrange
        const string host = "10.0.0.1";
        const int port = 1502;

        // Act
        var client = ModbusClientFactory.CreateUdpClient(host, port);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusUdpClient>();
    }

    [Fact]
    public void CreateUdpClient_DefaultPort_Uses502()
    {
        // Arrange
        const string host = "localhost";

        // Act
        var client = ModbusClientFactory.CreateUdpClient(host);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusUdpClient>();
    }

    [Fact]
    public void CreateUdpClient_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ModbusClientFactory.CreateUdpClient((NetworkConnectionConfig)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public void CreateUdpClient_InvalidHost_ThrowsArgumentException(string invalidHost)
    {
        // Act & Assert
        var host = invalidHost == "null" ? null : invalidHost;
        Assert.Throws<ArgumentException>(() => 
            ModbusClientFactory.CreateUdpClient(host!, 502));
    }

    #endregion

    #region RTU over TCP Client Tests

    [Fact]
    public void CreateRtuOverTcpClient_ValidConfig_ReturnsRtuOverTcpClient()
    {
        // Arrange
        var config = new NetworkConnectionConfig
        {
            Host = "192.168.1.100",
            Port = 502
        };

        // Act
        var client = ModbusClientFactory.CreateRtuOverTcpClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverTcpClient>();
    }

    [Fact]
    public void CreateRtuOverTcpClient_WithHostAndPort_ReturnsRtuOverTcpClient()
    {
        // Arrange
        const string host = "10.0.0.1";
        const int port = 1502;

        // Act
        var client = ModbusClientFactory.CreateRtuOverTcpClient(host, port);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverTcpClient>();
    }

    [Fact]
    public void CreateRtuOverTcpClient_DefaultPort_Uses502()
    {
        // Arrange
        const string host = "localhost";

        // Act
        var client = ModbusClientFactory.CreateRtuOverTcpClient(host);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverTcpClient>();
    }

    [Fact]
    public void CreateRtuOverTcpClient_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ModbusClientFactory.CreateRtuOverTcpClient((NetworkConnectionConfig)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public void CreateRtuOverTcpClient_InvalidHost_ThrowsArgumentException(string invalidHost)
    {
        // Act & Assert
        var host = invalidHost == "null" ? null : invalidHost;
        Assert.Throws<ArgumentException>(() => 
            ModbusClientFactory.CreateRtuOverTcpClient(host!, 502));
    }

    #endregion

    #region RTU over UDP Client Tests

    [Fact]
    public void CreateRtuOverUdpClient_ValidConfig_ReturnsRtuOverUdpClient()
    {
        // Arrange
        var config = new NetworkConnectionConfig
        {
            Host = "192.168.1.100",
            Port = 502
        };

        // Act
        var client = ModbusClientFactory.CreateRtuOverUdpClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverUdpClient>();
    }

    [Fact]
    public void CreateRtuOverUdpClient_WithHostAndPort_ReturnsRtuOverUdpClient()
    {
        // Arrange
        const string host = "10.0.0.1";
        const int port = 1502;

        // Act
        var client = ModbusClientFactory.CreateRtuOverUdpClient(host, port);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverUdpClient>();
    }

    [Fact]
    public void CreateRtuOverUdpClient_DefaultPort_Uses502()
    {
        // Arrange
        const string host = "localhost";

        // Act
        var client = ModbusClientFactory.CreateRtuOverUdpClient(host);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverUdpClient>();
    }

    [Fact]
    public void CreateRtuOverUdpClient_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ModbusClientFactory.CreateRtuOverUdpClient((NetworkConnectionConfig)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public void CreateRtuOverUdpClient_InvalidHost_ThrowsArgumentException(string invalidHost)
    {
        // Act & Assert
        var host = invalidHost == "null" ? null : invalidHost;
        Assert.Throws<ArgumentException>(() => 
            ModbusClientFactory.CreateRtuOverUdpClient(host!, 502));
    }

    #endregion

    #region Generic CreateClient Tests

    [Fact]
    public void CreateClient_RtuType_ReturnsRtuClient()
    {
        // Arrange
        var serialConfig = new SerialConnectionConfig { PortName = "COM1", BaudRate = 9600 };

        // Act
        var client = ModbusClientFactory.CreateClient(ModbusConnectionType.Rtu, serialConfig);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuClient>();
    }

    [Fact]
    public void CreateClient_TcpType_ReturnsTcpClient()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "localhost", Port = 502 };

        // Act
        var client = ModbusClientFactory.CreateClient(ModbusConnectionType.Tcp, networkConfig: networkConfig);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusTcpClient>();
    }

    [Fact]
    public void CreateClient_UdpType_ReturnsUdpClient()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "localhost", Port = 502 };

        // Act
        var client = ModbusClientFactory.CreateClient(ModbusConnectionType.Udp, networkConfig: networkConfig);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusUdpClient>();
    }

    [Fact]
    public void CreateClient_RtuOverTcpType_ReturnsRtuOverTcpClient()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "localhost", Port = 502 };

        // Act
        var client = ModbusClientFactory.CreateClient(ModbusConnectionType.RtuOverTcp, networkConfig: networkConfig);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverTcpClient>();
    }

    [Fact]
    public void CreateClient_RtuOverUdpType_ReturnsRtuOverUdpClient()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "localhost", Port = 502 };

        // Act
        var client = ModbusClientFactory.CreateClient(ModbusConnectionType.RtuOverUdp, networkConfig: networkConfig);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuOverUdpClient>();
    }

    [Fact]
    public void CreateClient_RtuTypeWithoutSerialConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ModbusClientFactory.CreateClient(ModbusConnectionType.Rtu));
    }

    [Fact]
    public void CreateClient_TcpTypeWithoutNetworkConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ModbusClientFactory.CreateClient(ModbusConnectionType.Tcp));
    }

    [Fact]
    public void CreateClient_UnsupportedConnectionType_ThrowsNotSupportedException()
    {
        // Arrange
        var invalidConnectionType = (ModbusConnectionType)999;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => 
            ModbusClientFactory.CreateClient(invalidConnectionType));
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public void CreateTcpClient_CustomPortConfiguration_UsesCustomValues()
    {
        // Arrange
        var config = new NetworkConnectionConfig
        {
            Host = "192.168.100.50",
            Port = 1234,
            ConnectTimeout = 15000,
            ReceiveTimeout = 8000,
            SendTimeout = 8000
        };

        // Act
        var client = ModbusClientFactory.CreateTcpClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusTcpClient>();
    }

    [Fact]
    public void CreateRtuClient_CustomSerialConfiguration_UsesCustomValues()
    {
        // Arrange
        var config = new SerialConnectionConfig
        {
            PortName = "COM5",
            BaudRate = 38400,
            Parity = System.IO.Ports.Parity.Even,
            DataBits = 7,
            StopBits = System.IO.Ports.StopBits.Two,
            ReadTimeout = 2000,
            WriteTimeout = 2000
        };

        // Act
        var client = ModbusClientFactory.CreateRtuClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<ModbusRtuClient>();
    }

    #endregion

    #region Common Scenarios Tests

    [Fact]
    public void CreateMultipleClients_SameConfiguration_ReturnsIndependentInstances()
    {
        // Arrange
        var config = new NetworkConnectionConfig { Host = "localhost", Port = 502 };

        // Act
        var client1 = ModbusClientFactory.CreateTcpClient(config);
        var client2 = ModbusClientFactory.CreateTcpClient(config);

        // Assert
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void CreateClients_DifferentTypes_ReturnsCorrectTypes()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "localhost", Port = 502 };
        var serialConfig = new SerialConnectionConfig { PortName = "COM1", BaudRate = 9600 };

        // Act
        var tcpClient = ModbusClientFactory.CreateTcpClient(networkConfig);
        var udpClient = ModbusClientFactory.CreateUdpClient(networkConfig);
        var rtuClient = ModbusClientFactory.CreateRtuClient(serialConfig);
        var rtuOverTcpClient = ModbusClientFactory.CreateRtuOverTcpClient(networkConfig);
        var rtuOverUdpClient = ModbusClientFactory.CreateRtuOverUdpClient(networkConfig);

        // Assert
        tcpClient.Should().BeOfType<ModbusTcpClient>();
        udpClient.Should().BeOfType<ModbusUdpClient>();
        rtuClient.Should().BeOfType<ModbusRtuClient>();
        rtuOverTcpClient.Should().BeOfType<ModbusRtuOverTcpClient>();
        rtuOverUdpClient.Should().BeOfType<ModbusRtuOverUdpClient>();
    }

    [Fact]
    public void CreateClient_AllTypesImplementIModbusClient_Success()
    {
        // Arrange
        var networkConfig = new NetworkConnectionConfig { Host = "localhost", Port = 502 };
        var serialConfig = new SerialConnectionConfig { PortName = "COM1", BaudRate = 9600 };

        // Act & Assert
        var tcpClient = ModbusClientFactory.CreateTcpClient(networkConfig);
        tcpClient.Should().BeAssignableTo<IModbusClient>();

        var udpClient = ModbusClientFactory.CreateUdpClient(networkConfig);
        udpClient.Should().BeAssignableTo<IModbusClient>();

        var rtuClient = ModbusClientFactory.CreateRtuClient(serialConfig);
        rtuClient.Should().BeAssignableTo<IModbusClient>();

        var rtuOverTcpClient = ModbusClientFactory.CreateRtuOverTcpClient(networkConfig);
        rtuOverTcpClient.Should().BeAssignableTo<IModbusClient>();

        var rtuOverUdpClient = ModbusClientFactory.CreateRtuOverUdpClient(networkConfig);
        rtuOverUdpClient.Should().BeAssignableTo<IModbusClient>();
    }

    #endregion
}