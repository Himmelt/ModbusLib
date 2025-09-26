using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Factories;
using ModbusLib.Models;

namespace ModbusLib.Tests.Clients;

public class ModbusClientUsageTests
{
    [Fact]
    public void CreateAndUseRtuClient_Example()
    {
        // Arrange
        var config = new SerialConnectionConfig
        {
            PortName = "COM1",
            BaudRate = 9600,
            Parity = System.IO.Ports.Parity.None,
            DataBits = 8,
            StopBits = System.IO.Ports.StopBits.One
        };

        // Act
        var client = ModbusClientFactory.CreateRtuClient(config);

        // Assert
        Assert.NotNull(client);
        Assert.IsAssignableFrom<IModbusClient>(client);
    }

    [Fact]
    public void CreateAndUseTcpClient_Example()
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
        Assert.IsAssignableFrom<IModbusClient>(client);
    }

    [Fact]
    public void ModbusFunction_Enum_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0x01, (byte)ModbusFunction.ReadCoils);
        Assert.Equal(0x02, (byte)ModbusFunction.ReadDiscreteInputs);
        Assert.Equal(0x03, (byte)ModbusFunction.ReadHoldingRegisters);
        Assert.Equal(0x04, (byte)ModbusFunction.ReadInputRegisters);
        Assert.Equal(0x05, (byte)ModbusFunction.WriteSingleCoil);
        Assert.Equal(0x06, (byte)ModbusFunction.WriteSingleRegister);
        Assert.Equal(0x0F, (byte)ModbusFunction.WriteMultipleCoils);
        Assert.Equal(0x10, (byte)ModbusFunction.WriteMultipleRegisters);
        Assert.Equal(0x17, (byte)ModbusFunction.ReadWriteMultipleRegisters);
    }
}