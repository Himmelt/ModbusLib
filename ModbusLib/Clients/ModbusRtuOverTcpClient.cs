using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

public class ModbusRtuOverTcpClient(NetworkConfig config) : ModbusClientBase {

    public NetworkConfig NetworkConfig => config;

    protected override IModbusProtocol Protocol { get; set; } = new RtuProtocol();
    protected override IModbusTransport Transport { get; set; } = new TcpTransport(config);

    public ModbusRtuOverTcpClient(string host, int remotePort = 502) : this(new NetworkConfig { Host = host, RemotePort = remotePort }) { }
}