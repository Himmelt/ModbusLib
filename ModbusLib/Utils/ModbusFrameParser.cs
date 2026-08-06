using ModbusLib.Enums;

namespace ModbusLib.Utils;

/// <summary>
/// 依据已接收的部分字节确定 Modbus 响应帧的完整长度。
/// </summary>
internal static class ModbusFrameParser {

    /// <summary>
    /// 尝试确定响应帧长度；数据不足或无法识别时返回 null。
    /// </summary>
    public static int? TryGetResponseFrameLength(ReadOnlySpan<byte> data, ProtocolType protocol) {
        if (protocol == ProtocolType.Tcp) {
            // MBAP 帧: 事务ID(2) + 协议ID(2) + 长度(2) + 单元ID + PDU
            if (data.Length < 6) return null;
            var length = (data[4] << 8) | data[5];
            return 6 + length;
        }

        // RTU 帧: 设备地址 + 功能码 + ...
        if (data.Length < 2) return null;
        var functionCode = data[1];

        if ((functionCode & 0x80) != 0) {
            // 异常响应: 设备地址 + 功能码(异常) + 异常码 + CRC
            return 5;
        }

        return functionCode switch {
            (byte)ModbusFunction.ReadCoils or (byte)ModbusFunction.ReadDiscreteInputs
                or (byte)ModbusFunction.ReadHoldingRegisters or (byte)ModbusFunction.ReadInputRegisters
                or (byte)ModbusFunction.ReadWriteMultipleRegisters =>
                data.Length < 3 ? null : 5 + data[2],
            (byte)ModbusFunction.WriteSingleCoil or (byte)ModbusFunction.WriteSingleRegister
                or (byte)ModbusFunction.WriteMultipleCoils or (byte)ModbusFunction.WriteMultipleRegisters =>
                8,
            _ => null
        };
    }
}
