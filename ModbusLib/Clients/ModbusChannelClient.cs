using ModbusLib.Enums;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

public class ModbusChannelClient(ChannelSession session, ProtocolType protocol = ProtocolType.Tcp) : ModbusClientBase {
    protected override IModbusProtocol Protocol { get; set; } = protocol.GetProtocol();
    protected override IModbusTransport Transport { get; set; } = new ChannelTransport(session);
}