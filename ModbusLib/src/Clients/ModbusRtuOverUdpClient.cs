using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using System.Diagnostics.CodeAnalysis;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus RTU over UDP客户端
/// </summary>
public class ModbusRtuOverUdpClient : ModbusClientBase
{
    [SuppressMessage("CodeQuality", "IDE0079")]
    [SuppressMessage("Reliability", "CA2000:丢失范围之前释放对象", Justification = "在基类中统一释放")]
    public ModbusRtuOverUdpClient(NetworkConnectionConfig config)
        : base(new UdpTransport(config), new RtuProtocol())
    {
    }

    public ModbusRtuOverUdpClient(UdpTransport transport)
        : base(transport, new RtuProtocol())
    {
    }
}