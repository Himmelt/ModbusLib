namespace ModbusLib.Enums
{
    /// <summary>
    /// Modbus字节序模式
    /// </summary>
    public enum ModbusEndianness
    {
        /// <summary>
        /// 大端字节序 (Modbus标准, 高位字节在前)
        /// </summary>
        BigEndian,
        
        /// <summary>
        /// 小端字节序 (低位字节在前)
        /// </summary>
        LittleEndian,
        
        /// <summary>
        /// 中端字节序 (字内小端，字间大端)
        /// </summary>
        MidLittleEndian
    }
}