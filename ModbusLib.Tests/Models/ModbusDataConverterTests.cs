using ModbusLib.Enums;
using ModbusLib.Models;

namespace ModbusLib.Tests.Models;

public class ModbusDataConverterTests
{
    [Fact]
    public void GetRegisterCount_WithFloat_ReturnsCorrectCount()
    {
        // Act
        var result = ModbusDataConverter.GetRegisterCount<float>();

        // Assert
        Assert.Equal(2, result); // float is 4 bytes = 2 registers
    }

    [Fact]
    public void GetRegisterCount_WithUShort_ReturnsCorrectCount()
    {
        // Act
        var result = ModbusDataConverter.GetRegisterCount<ushort>();

        // Assert
        Assert.Equal(1, result); // ushort is 2 bytes = 1 register
    }

    [Fact]
    public void GetTotalRegisterCount_WithFloatArray_ReturnsCorrectCount()
    {
        // Arrange
        var elementCount = 3;

        // Act
        var result = ModbusDataConverter.GetTotalRegisterCount<float>(elementCount);

        // Assert
        Assert.Equal(6, result); // 3 floats * 2 registers each = 6 registers
    }
}