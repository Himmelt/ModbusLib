using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

public class ModbusRtuOverPipeClient(PipeSession session) : ModbusClientBase {
    protected override IModbusProtocol Protocol { get; set; } = new RtuProtocol();
    protected override IModbusTransport Transport { get; set; } = new PipeTransport(session);
}