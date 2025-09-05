using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus RTU over TCP客户端
/// </summary>
public class ModbusRtuOverTcpClient : ModbusClientBase
{
    public ModbusRtuOverTcpClient(NetworkConnectionConfig config)
        : base(new TcpTransport(config), new RtuProtocol())
    {
    }

    public ModbusRtuOverTcpClient(TcpTransport transport)
        : base(transport, new RtuProtocol())
    {
    }
}