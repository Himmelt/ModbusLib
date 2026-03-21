using ModbusLib.Interfaces;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using System.IO.Pipelines;

namespace ModbusLib.Clients;

public class ModbusPipeClient(Pipe pipeIn, Pipe pipeOut, IModbusProtocol protocol, int timeout = 5000) : ModbusClientBase(new PipeTransport(pipeIn, pipeOut, timeout), protocol) {

    public static IModbusClient CreateTcpClient(Pipe pipeIn, Pipe pipeOut, int timeout = 5000) {
        return new ModbusPipeClient(pipeIn, pipeOut, new TcpProtocol(), timeout);
    }

    public static IModbusClient CreateRtuClient(Pipe pipeIn, Pipe pipeOut, int timeout = 5000) {
        return new ModbusPipeClient(pipeIn, pipeOut, new RtuProtocol(), timeout);
    }
}