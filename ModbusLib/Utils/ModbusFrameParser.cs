using ModbusLib.Enums;

namespace ModbusLib.Utils;

/// <summary>
/// RTU 请求所期望的响应身份，用于把“从通道里读到的一帧”与“本次请求”做相关性校验。
/// RTU 帧没有事务标识，只能靠 设备地址 + 功能码 + 帧长 来识别，
/// 这是 RTU 下唯一可行的防“在途迟到帧抢占”手段。
/// （MBAP 有事务号且在协议层已校验、并可经重试自愈，因此不走这里。）
/// </summary>
internal readonly record struct RtuResponseExpectation(
    byte UnitId,
    byte Function,
    int? FrameLength) {

    /// <summary>
    /// 判断某段“位于帧起始处”的字节是否可能是本次请求的响应开头（即首字节为设备地址）。
    /// 用于丢弃分片残留：上一轮超时可能只消费掉了半帧，剩下的帧尾会以“帧起始”的姿态出现，
    /// 其首字节不可能是本次请求的设备地址。
    /// </summary>
    public bool CouldStartFrame(ReadOnlySpan<byte> data) => data.IsEmpty || data[0] == UnitId;

    /// <summary>
    /// 判断一帧完整响应是否确实是本次请求的响应。
    /// </summary>
    public bool Matches(ReadOnlySpan<byte> frame) {
        // RTU: 设备地址 + 功能码 + ...
        if (frame.Length < 5) return false;
        if (frame[0] != UnitId) return false;

        var function = frame[1];
        if ((function & 0x80) != 0) {
            // 异常响应：设备地址 + 功能码(异常) + 异常码 + CRC，固定 5 字节
            return function == (byte)(Function | 0x80) && frame.Length == 5;
        }

        if (function != Function) return false;
        // 帧长由请求推导（读响应 = 5 + 字节数，写响应 = 8），长度不符即不是本次请求的响应
        return FrameLength is not int expected || frame.Length == expected;
    }
}

/// <summary>
/// 依据已接收的部分字节确定 Modbus 响应帧的完整长度。
/// </summary>
internal static class ModbusFrameParser {

    /// <summary>
    /// 从 RTU 请求帧推导出期望的响应身份；请求帧长度不足或功能码不受支持时返回 null
    /// （此时只做“发请求前排空”）。
    /// </summary>
    public static RtuResponseExpectation? TryGetRtuResponseExpectation(ReadOnlySpan<byte> request) {
        // RTU 请求: 设备地址 + 功能码 + ... + CRC(2)，最短 8 字节
        if (request.Length < 8) return null;

        var function = request[1];
        var quantity = (ushort)((request[4] << 8) | request[5]);
        int? responseLength = function switch {
            (byte)ModbusFunction.ReadCoils or (byte)ModbusFunction.ReadDiscreteInputs => 5 + (quantity + 7) / 8,
            (byte)ModbusFunction.ReadHoldingRegisters or (byte)ModbusFunction.ReadInputRegisters => 5 + quantity * 2,
            (byte)ModbusFunction.WriteSingleCoil or (byte)ModbusFunction.WriteSingleRegister
                or (byte)ModbusFunction.WriteMultipleCoils or (byte)ModbusFunction.WriteMultipleRegisters => 8,
            (byte)ModbusFunction.ReadWriteMultipleRegisters => 5 + quantity * 2,
            _ => null
        };
        return new RtuResponseExpectation(request[0], function, responseLength);
    }

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
