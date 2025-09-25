using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using System.Diagnostics.CodeAnalysis;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus RTU over TCP客户端
/// </summary>
public class ModbusRtuOverTcpClient : ModbusClientBase {
    [SuppressMessage("CodeQuality", "IDE0079")]
    [SuppressMessage("Reliability", "CA2000:丢失范围之前释放对象", Justification = "在基类中统一释放")]
    public ModbusRtuOverTcpClient(NetworkConnectionConfig config)
        : base(new TcpTransport(config), new RtuProtocol()) {
    }

    public ModbusRtuOverTcpClient(TcpTransport transport)
        : base(transport, new RtuProtocol()) {
    }
}