using ModbusLib.Utils;

namespace ModbusLib.Tests.Utils;

public class Crc16UtilsTests {

    [Fact]
    public void CalculateCrc16() {
        var data = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var expected = CalculateCRC(data);
        var result = Crc16Utils.CalculateCrc16(data);
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
