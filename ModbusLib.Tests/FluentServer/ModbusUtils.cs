namespace FluentModbus;

internal static class ModbusUtils {
    public static ushort CalculateCRC(Memory<byte> buffer) {
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

    public static bool DetectRequestFrame(byte unitIdentifier, Memory<byte> frame) {
        var span = frame.Span;

        if (span.Length < 4)
            return false;

        if (unitIdentifier != 255) {
            var newUnitIdentifier = span[0];

            if (newUnitIdentifier != unitIdentifier)
                return false;
        }

        var crcBytes = span.Slice(span.Length - 2, 2);
        var actualCRC = unchecked((ushort)((crcBytes[1] << 8) + crcBytes[0]));
        var expectedCRC = CalculateCRC(frame[..^2]);

        if (actualCRC != expectedCRC)
            return false;

        return true;
    }
}