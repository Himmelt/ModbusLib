using ModbusLib.Interfaces;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace ModbusLib.Clients;

public class ModbusPipeClient : ModbusClientBase {
    [SuppressMessage("CodeQuality", "IDE0079")]
    [SuppressMessage("Reliability", "CA2000:丢失范围之前释放对象", Justification = "在基类中统一释放")]
    public ModbusPipeClient(Pipe pipeIn, Pipe pipeOut, IModbusProtocol protocol, int timeout = 5000)
        : base(new PipeTransport(pipeIn, pipeOut, timeout), protocol) {
    }

    [SuppressMessage("CodeQuality", "IDE0079")]
    [SuppressMessage("Reliability", "CA2000:丢失范围之前释放对象", Justification = "在基类中统一释放")]
    public ModbusPipeClient(Pipe pipeIn, Pipe pipeOut, IModbusProtocol protocol, PipeTransport transport)
        : base(transport, protocol) {
    }

    public static IModbusClient CreateTcpClient(Pipe pipeIn, Pipe pipeOut, int timeout = 5000) {
        return new ModbusPipeClient(pipeIn, pipeOut, new TcpProtocol(), timeout);
    }

    public static IModbusClient CreateRtuClient(Pipe pipeIn, Pipe pipeOut, int timeout = 5000) {
        return new ModbusPipeClient(pipeIn, pipeOut, new RtuProtocol(), timeout);
    }
}