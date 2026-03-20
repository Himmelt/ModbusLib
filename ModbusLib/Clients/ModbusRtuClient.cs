using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using System.Diagnostics.CodeAnalysis;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus RTU客户端
/// </summary>
public class ModbusRtuClient : ModbusClientBase {
    [SuppressMessage("CodeQuality", "IDE0079")]
    [SuppressMessage("Reliability", "CA2000:丢失范围之前释放对象", Justification = "在基类中统一释放")]
    public ModbusRtuClient(SerialConnectionConfig config)
        : base(new SerialTransport(config), new RtuProtocol()) {
    }

    public ModbusRtuClient(SerialTransport transport)
        : base(transport, new RtuProtocol()) {
    }
}