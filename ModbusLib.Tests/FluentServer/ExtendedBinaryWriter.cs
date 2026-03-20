namespace FluentModbus;

public class ExtendedBinaryWriter : BinaryWriter {
    public ExtendedBinaryWriter(Stream stream) : base(stream) {
    }

    private void WriteReverse(byte[] data) {
        Array.Reverse(data);
        base.Write(data);
    }

    public void WriteReverse(short value) {
        WriteReverse(BitConverter.GetBytes(value));
    }

    public void WriteReverse(ushort value) {
        WriteReverse(BitConverter.GetBytes(value));
    }

    public void WriteReverse(int value) {
        WriteReverse(BitConverter.GetBytes(value));
    }

    public void WriteReverse(uint value) {
        WriteReverse(BitConverter.GetBytes(value));
    }

    public void WriteReverse(long value) {
        WriteReverse(BitConverter.GetBytes(value));
    }

    public void WriteReverse(ulong value) {
        WriteReverse(BitConverter.GetBytes(value));
    }

    public void WriteReverse(float value) {
        WriteReverse(BitConverter.GetBytes(value));
    }

    public void WriteReverse(double value) {
        WriteReverse(BitConverter.GetBytes(value));
    }
}