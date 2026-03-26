using ModbusLib.Interfaces;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

public class ModbusChannelClient(ChannelSession session, IModbusProtocol protocol, int timeout = 5000) : ModbusClientBase(new ChannelTransport(session, timeout), protocol) {

    public static IModbusClient CreateTcpClient(ChannelSession session, int timeout = 5000) {
        return new ModbusChannelClient(session, new TcpProtocol(), timeout);
    }

    public static IModbusClient CreateRtuClient(ChannelSession session, int timeout = 5000) {
        return new ModbusChannelClient(session, new RtuProtocol(), timeout);
    }
}
