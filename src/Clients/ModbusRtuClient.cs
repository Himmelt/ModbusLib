using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus RTU客户端
/// </summary>
public class ModbusRtuClient : ModbusClientBase
{
    public ModbusRtuClient(SerialConnectionConfig config)
        : base(new SerialTransport(config), new RtuProtocol())
    {
    }

    public ModbusRtuClient(SerialTransport transport)
        : base(transport, new RtuProtocol())
    {
    }
}