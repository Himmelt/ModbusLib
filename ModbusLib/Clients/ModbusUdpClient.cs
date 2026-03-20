using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using System.Diagnostics.CodeAnalysis;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus UDP客户端
/// </summary>
public class ModbusUdpClient : ModbusClientBase {
    [SuppressMessage("CodeQuality", "IDE0079")]
    [SuppressMessage("Reliability", "CA2000:丢失范围之前释放对象", Justification = "在基类中统一释放")]
    public ModbusUdpClient(NetworkConnectionConfig config) : base(new UdpTransport(config), new TcpProtocol()) {
        // UDP使用TCP协议格式
    }

    public ModbusUdpClient(UdpTransport transport) : base(transport, new TcpProtocol()) {
    }
}