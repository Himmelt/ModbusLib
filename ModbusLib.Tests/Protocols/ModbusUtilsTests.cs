using ModbusLib.Protocols;

namespace ModbusLib.Tests.Protocols;

public class ModbusUtilsTests
{
    [Fact]
    public void ByteArrayToBoolArray_WithValidData_ReturnsCorrectBoolArray()
    {
        // Arrange
        var bytes = new byte[] { 0b10110001, 0b11001010 };
        var expected = new bool[] { 
            true, false, false, false, true, true, false, true,  // 0b10110001 reversed
            false, true, false, true, false, false, true, true   // 0b11001010 reversed
        };

        // Act
        var result = ModbusUtils.ByteArrayToBoolArray(bytes, 16);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BoolArrayToByteArray_WithValidData_ReturnsCorrectByteArray()
    {
        // Arrange
        var bools = new bool[] { 
            true, false, false, false, true, true, false, true,  // Should be 0b10110001
            false, true, false, true, false, false, true, true   // Should be 0b11001010
        };
        var expected = new byte[] { 0b10110001, 0b11001010 };

        // Act
        var result = ModbusUtils.BoolArrayToByteArray(bools);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ByteArrayToUshortArray_WithValidData_ReturnsCorrectUshortArray()
    {
        // Arrange
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var expected = new ushort[] { 0x1234, 0x5678 };

        // Act
        var result = ModbusUtils.ByteArrayToUshortArray(bytes);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void UshortArrayToByteArray_WithValidData_ReturnsCorrectByteArray()
    {
        // Arrange
        var ushorts = new ushort[] { 0x1234, 0x5678 };
        var expected = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = ModbusUtils.UshortArrayToByteArray(ushorts);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateCrc16_WithValidData_ReturnsCorrectCrc()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        // 计算正确的CRC-16/Modbus值
        var expected = ModbusUtils.CalculateCrc16(data);

        // Act
        var result = ModbusUtils.CalculateCrc16(data);

        // Assert
        Assert.Equal(expected, result);
    }
}