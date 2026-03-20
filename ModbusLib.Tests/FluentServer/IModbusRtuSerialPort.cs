namespace ModbusLib.Tests.FluentServer;

public interface IModbusRtuSerialPort {
    int Read(byte[] buffer, int offset, int count);

    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token);

    void Write(byte[] buffer, int offset, int count);

    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token);

    void Open();

    void Close();

    string PortName { get; }

    bool IsOpen { get; }
}