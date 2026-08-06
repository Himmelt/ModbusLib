namespace ModbusLib.Utils;

/// <summary>
/// Modbus CRC16 工具类
/// </summary>
public static class Crc16Utils {
    /// <summary>
    /// 计算CRC-16/Modbus校验码
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>CRC校验码</returns>
    public static ushort CalculateCrc16(byte[] data) {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        return CalculateCrc16(data, 0, data.Length);
    }

    /// <summary>
    /// 计算CRC-16/Modbus校验码
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="offset">起始位置</param>
    /// <param name="length">长度</param>
    /// <returns>CRC校验码</returns>
    public static ushort CalculateCrc16(byte[] data, int offset, int length) {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        ushort crc = 0xFFFF;

        for (int i = offset; i < offset + length; i++) {
            crc ^= data[i];

            for (int j = 0; j < 8; j++) {
                if ((crc & 0x0001) != 0) {
                    crc >>= 1;
                    crc ^= 0xA001;
                } else {
                    crc >>= 1;
                }
            }
        }

        return crc;
    }

    /// <summary>
    /// 验证CRC校验码
    /// </summary>
    /// <param name="data">包含CRC的完整数据</param>
    /// <returns>是否有效</returns>
    public static bool ValidateCrc16(byte[] data) {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        // 最短合法 RTU 帧为 5 字节（设备地址 + 功能码 + 异常码 + CRC 2 字节）
        if (data.Length < 5)
            return false;

        var dataLength = data.Length - 2;
        var expectedCrc = CalculateCrc16(data, 0, dataLength);
        var actualCrc = (ushort)(data[dataLength] | data[dataLength + 1] << 8);

        return expectedCrc == actualCrc;
    }
}
