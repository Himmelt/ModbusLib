using ModbusLib.Enums;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

public class ModbusTcpClient(NetworkConfig config, ProtocolType protocol = ProtocolType.Tcp) : ModbusClientBase {

    public NetworkConfig NetworkConfig => config;

    protected override IModbusProtocol Protocol { get; set; } = protocol.GetProtocol();
    protected override IModbusTransport Transport { get; set; } = new TcpTransport(config, protocol);

    public ModbusTcpClient(string host, int remotePort = 502, ProtocolType protocol = ProtocolType.Tcp) : this(new NetworkConfig { RemoteHost = host, RemotePort = remotePort }, protocol) { }
}
