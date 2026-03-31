using ModbusLib.Interfaces;
using ModbusLib.Protocols;

namespace ModbusLib.Enums;

public enum ProtocolType {
    Rtu,
    Tcp,
}

public static class ProtocolTypeExtensions {
    public static IModbusProtocol GetProtocol(this ProtocolType protocolType) {
        return protocolType switch {
            ProtocolType.Rtu => new RtuProtocol(),
            ProtocolType.Tcp => new TcpProtocol(),
            _ => throw new ArgumentOutOfRangeException(nameof(protocolType))
        };
    }
}