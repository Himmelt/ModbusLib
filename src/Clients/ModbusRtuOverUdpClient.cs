using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus RTU over UDP客户端
/// </summary>
public class ModbusRtuOverUdpClient : ModbusClientBase
{
    public ModbusRtuOverUdpClient(NetworkConnectionConfig config)
        : base(new UdpTransport(config), new RtuProtocol())
    {
    }

    public ModbusRtuOverUdpClient(UdpTransport transport)
        : base(transport, new RtuProtocol())
    {
    }
}