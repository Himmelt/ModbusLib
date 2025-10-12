using ModbusLib.Protocols;

namespace ModbusLib.Tests.Protocols;

public class ModbusUtilsTests {
    [Fact]
    public void ByteArrayToBoolArray() {
        var bytes = new byte[] { 0b10110001, 0b11001010 };
        var expected = new bool[] {
            true, false, false, false, true, true, false, true,  // 0b10110001
            false, true, false, true, false, false, true, true   // 0b11001010
        };

        var result = ModbusUtils.ByteArrayToBoolArray(bytes, 16);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BoolArrayToByteArray() {
        var bools = new bool[] {
            true, false, false, false, true, true, false, true,  // 0b10110001
            false, true, false, true, false, false, true, true   // 0b11001010
        };
        var expected = new byte[] { 0b10110001, 0b11001010 };

        var result = ModbusUtils.BoolArrayToByteArray(bools);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ByteArrayToUshortArray() {
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var expected = new ushort[] { 0x1234, 0x5678 };
        var result = ModbusUtils.ByteArrayToUshortArray(bytes);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void UshortArrayToByteArray() {
        var ushorts = new ushort[] { 0x1234, 0x5678 };
        var expected = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var result = ModbusUtils.UshortArrayToByteArray(ushorts);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateCrc16() {
        var data = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var expected = CalculateCRC(data);
        var result = ModbusUtils.CalculateCrc16(data);
        Assert.Equal(expected, result);
    }

    private static ushort CalculateCRC(Memory<byte> buffer) {
        var span = buffer.Span;
        ushort crc = 0xFFFF;

        foreach (var value in span) {
            crc ^= value;

            for (int i = 0; i < 8; i++) {
                if ((crc & 0x0001) != 0) {
                    crc >>= 1;
                    crc ^= 0xA001;
                } else {
                    crc >>= 1;
                }
            }
        }

        return crc;
    }
}
