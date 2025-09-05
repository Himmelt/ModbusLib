namespace ModbusLib.Protocols;

/// <summary>
/// Modbus工具类
/// </summary>
public static class ModbusUtils
{
    /// <summary>
    /// 计算CRC-16/Modbus校验码
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>CRC校验码</returns>
    public static ushort CalculateCrc16(byte[] data)
    {
        return CalculateCrc16(data, 0, data.Length);
    }

    /// <summary>
    /// 计算CRC-16/Modbus校验码
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="offset">起始位置</param>
    /// <param name="length">长度</param>
    /// <returns>CRC校验码</returns>
    public static ushort CalculateCrc16(byte[] data, int offset, int length)
    {
        ushort crc = 0xFFFF;
        
        for (int i = offset; i < offset + length; i++)
        {
            crc ^= data[i];
            
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
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
    public static bool ValidateCrc16(byte[] data)
    {
        if (data.Length < 3)
            return false;

        var dataLength = data.Length - 2;
        var expectedCrc = CalculateCrc16(data, 0, dataLength);
        var actualCrc = (ushort)(data[dataLength] | (data[dataLength + 1] << 8));
        
        return expectedCrc == actualCrc;
    }

    /// <summary>
    /// 将布尔数组转换为字节数组（用于线圈数据）
    /// </summary>
    /// <param name="bits">布尔数组</param>
    /// <returns>字节数组</returns>
    public static byte[] BoolArrayToByteArray(bool[] bits)
    {
        var byteCount = (bits.Length + 7) / 8;
        var bytes = new byte[byteCount];
        
        for (int i = 0; i < bits.Length; i++)
        {
            if (bits[i])
            {
                var byteIndex = i / 8;
                var bitIndex = i % 8;
                bytes[byteIndex] |= (byte)(1 << bitIndex);
            }
        }
        
        return bytes;
    }

    /// <summary>
    /// 将字节数组转换为布尔数组（用于线圈数据）
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="bitCount">位数</param>
    /// <returns>布尔数组</returns>
    public static bool[] ByteArrayToBoolArray(byte[] bytes, int bitCount)
    {
        var bits = new bool[bitCount];
        
        for (int i = 0; i < bitCount; i++)
        {
            var byteIndex = i / 8;
            var bitIndex = i % 8;
            
            if (byteIndex < bytes.Length)
            {
                bits[i] = (bytes[byteIndex] & (1 << bitIndex)) != 0;
            }
        }
        
        return bits;
    }

    /// <summary>
    /// 将ushort数组转换为字节数组（大端序）
    /// </summary>
    /// <param name="values">ushort数组</param>
    /// <returns>字节数组</returns>
    public static byte[] UshortArrayToByteArray(ushort[] values)
    {
        var bytes = new byte[values.Length * 2];
        
        for (int i = 0; i < values.Length; i++)
        {
            bytes[i * 2] = (byte)(values[i] >> 8);
            bytes[i * 2 + 1] = (byte)(values[i] & 0xFF);
        }
        
        return bytes;
    }

    /// <summary>
    /// 将字节数组转换为ushort数组（大端序）
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <returns>ushort数组</returns>
    public static ushort[] ByteArrayToUshortArray(byte[] bytes)
    {
        var values = new ushort[bytes.Length / 2];
        
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (ushort)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);
        }
        
        return values;
    }
}