using ModbusLib.Enums;

namespace ModbusLib.Tests.Enums;

public class ModbusConnectionTypeTests
{
    [Fact]
    public void ModbusConnectionType_Enum_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)ModbusConnectionType.Rtu);
        Assert.Equal(1, (int)ModbusConnectionType.Tcp);
        Assert.Equal(2, (int)ModbusConnectionType.Udp);
        Assert.Equal(3, (int)ModbusConnectionType.RtuOverTcp);
        Assert.Equal(4, (int)ModbusConnectionType.RtuOverUdp);
    }

    [Fact]
    public void ModbusConnectionType_Enum_HasCorrectNumberOfValues()
    {
        // Arrange
        var enumValues = Enum.GetValues<ModbusConnectionType>();

        // Assert
        Assert.Equal(5, enumValues.Length);
    }
}