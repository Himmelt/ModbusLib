using ModbusLib.Enums;

namespace ModbusLib.Tests.Enums;

public class ModbusExceptionCodeTests
{
    [Fact]
    public void ModbusExceptionCode_Enum_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0x00, (byte)ModbusExceptionCode.None);
        Assert.Equal(0x01, (byte)ModbusExceptionCode.IllegalFunction);
        Assert.Equal(0x02, (byte)ModbusExceptionCode.IllegalDataAddress);
        Assert.Equal(0x03, (byte)ModbusExceptionCode.IllegalDataValue);
        Assert.Equal(0x04, (byte)ModbusExceptionCode.SlaveDeviceFailure);
        Assert.Equal(0x05, (byte)ModbusExceptionCode.Acknowledge);
        Assert.Equal(0x06, (byte)ModbusExceptionCode.SlaveDeviceBusy);
        Assert.Equal(0x08, (byte)ModbusExceptionCode.MemoryParityError);
        Assert.Equal(0x0A, (byte)ModbusExceptionCode.GatewayPathUnavailable);
        Assert.Equal(0x0B, (byte)ModbusExceptionCode.GatewayTargetDeviceFailedToRespond);
    }

    [Fact]
    public void ModbusExceptionCode_Enum_HasCorrectNumberOfValues()
    {
        // Arrange
        var enumValues = Enum.GetValues<ModbusExceptionCode>();

        // Assert
        Assert.Equal(10, enumValues.Length);
    }
}