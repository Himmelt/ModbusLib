using FluentModbus;
using System.Buffers;

namespace ModbusLib.Tests.FluentServer;

internal class ModbusFrameBuffer : IDisposable {
    public ModbusFrameBuffer(int size) {
        Buffer = ArrayPool<byte>.Shared.Rent(size);

        Writer = new ExtendedBinaryWriter(new MemoryStream(Buffer));
        Reader = new ExtendedBinaryReader(new MemoryStream(Buffer));
    }

    public byte[] Buffer { get; }

    public ExtendedBinaryWriter Writer { get; }
    public ExtendedBinaryReader Reader { get; }

    private bool _disposedValue = false;

    protected virtual void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                Writer.Dispose();
                Reader.Dispose();

                ArrayPool<byte>.Shared.Return(Buffer);
            }

            _disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(true);
    }
}