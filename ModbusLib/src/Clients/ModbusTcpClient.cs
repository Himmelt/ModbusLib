using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus TCP客户端
/// </summary>
public class ModbusTcpClient : ModbusClientBase
{
    public ModbusTcpClient(NetworkConnectionConfig config)
        : base(new TcpTransport(config), new TcpProtocol())
    {
    }

    public ModbusTcpClient(TcpTransport transport)
        : base(transport, new TcpProtocol())
    {
    }
}