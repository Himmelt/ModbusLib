using ModbusLib.Enums;

namespace ModbusLib.Tests.Enums;

public class ModbusFunctionTests
{
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

    [Fact]
    public void ModbusFunction_Enum_HasCorrectNumberOfValues()
    {
        // Arrange
        var enumValues = Enum.GetValues<ModbusFunction>();

        // Assert
        Assert.Equal(9, enumValues.Length);
    }
}