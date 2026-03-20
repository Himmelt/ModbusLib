using System.Runtime.InteropServices;

namespace FluentModbus;

public class ExtendedBinaryReader : BinaryReader {
    public ExtendedBinaryReader(Stream stream) : base(stream) {
    }

    public short ReadInt16Reverse() {
        return ReadReverse<short>(BitConverter.GetBytes(ReadInt16()));
    }

    public ushort ReadUInt16Reverse() {
        return ReadReverse<ushort>(BitConverter.GetBytes(ReadUInt16()));
    }

    public int ReadInt32Reverse() {
        return ReadReverse<int>(BitConverter.GetBytes(ReadInt32()));
    }

    public uint ReadUInt32Reverse() {
        return ReadReverse<uint>(BitConverter.GetBytes(ReadUInt32()));
    }

    public long ReadInt64Reverse() {
        return ReadReverse<long>(BitConverter.GetBytes(ReadInt64()));
    }

    public ulong ReadUInt64Reverse() {
        return ReadReverse<ulong>(BitConverter.GetBytes(ReadUInt64()));
    }

    public float ReadFloat32Reverse() {
        return ReadReverse<float>(BitConverter.GetBytes(ReadSingle()));
    }

    public double ReadFloat64Reverse() {
        return ReadReverse<double>(BitConverter.GetBytes(ReadDouble()));
    }

    private T ReadReverse<T>(byte[] data) where T : struct {
        data.AsSpan().Reverse();
        return MemoryMarshal.Cast<byte, T>(data)[0];
    }
}