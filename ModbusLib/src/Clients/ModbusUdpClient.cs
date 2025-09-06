using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus UDP客户端
/// </summary>
public class ModbusUdpClient : ModbusClientBase
{
    public ModbusUdpClient(NetworkConnectionConfig config)
        : base(new UdpTransport(config), new TcpProtocol()) // UDP使用TCP协议格式
    {
    }

    public ModbusUdpClient(UdpTransport transport)
        : base(transport, new TcpProtocol())
    {
    }
}