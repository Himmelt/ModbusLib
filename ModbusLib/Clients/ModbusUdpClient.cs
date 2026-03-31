using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

public class ModbusUdpClient(NetworkConfig config) : ModbusClientBase {

    public NetworkConfig NetworkConfig => config;

    protected override IModbusProtocol Protocol { get; set; } = new TcpProtocol();
    protected override IModbusTransport Transport { get; set; } = new UdpTransport(config);

    public ModbusUdpClient(string host, int remotePort = 502) : this(new NetworkConfig { Host = host, RemotePort = remotePort }) { }
}