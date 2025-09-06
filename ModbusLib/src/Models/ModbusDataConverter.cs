using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ModbusLib.Enums;

namespace ModbusLib.Models
{
    /// <summary>
    /// Modbus数据类型转换器
    /// </summary>
    public static class ModbusDataConverter
    {
        /// <summary>
        /// 计算指定类型需要的寄存器数量
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>寄存器数量</returns>
        public static int GetRegisterCount<T>() where T : unmanaged
        {
            return (Unsafe.SizeOf<T>() + 1) / 2; // 向上取整
        }
        
        /// <summary>
        /// 计算指定数量元素需要的寄存器数量
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="count">T类型元素数量</param>
        /// <returns>所需的寄存器数量 (例如: 10个byte需要5个寄存器，5个int需要10个寄存器)</returns>
        public static int GetTotalRegisterCount<T>(int count) where T : unmanaged
        {
            if (count <= 0)
                return 0;
                
            return (Unsafe.SizeOf<T>() * count + 1) / 2;
        }
        
        /// <summary>
        /// 将泛型数组转换为字节数组
        /// </summary>
        /// <typeparam name="T">源类型</typeparam>
        /// <param name="values">源数组</param>
        /// <param name="endianness">字节序</param>
        /// <returns>字节数组</returns>
        public static byte[] ToBytes<T>(T[] values, ModbusEndianness endianness = ModbusEndianness.BigEndian) 
            where T : unmanaged
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
                
            var byteCount = Unsafe.SizeOf<T>() * values.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
            
            try
            {
                var span = buffer.AsSpan(0, byteCount);
                var sourceSpan = MemoryMarshal.Cast<T, byte>(values.AsSpan());
                sourceSpan.CopyTo(span);
                
                // 根据字节序调整
                ApplyEndianness<T>(span, endianness);
                
                return span.ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        
        /// <summary>
        /// 将字节数组转换为泛型数组
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="bytes">源字节数组</param>
        /// <param name="count">元素数量</param>
        /// <param name="endianness">字节序</param>
        /// <returns>泛型数组</returns>
        public static T[] FromBytes<T>(byte[] bytes, int count, ModbusEndianness endianness = ModbusEndianness.BigEndian) 
            where T : unmanaged
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (count < 0)
                throw new ArgumentException("元素数量不能为负数", nameof(count));
                
            var expectedSize = Unsafe.SizeOf<T>() * count;
            if (bytes.Length < expectedSize)
                throw new ArgumentException($"字节数组长度不足，需要 {expectedSize} 字节，实际 {bytes.Length} 字节");
                
            var workingBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);
            try
            {
                var span = workingBuffer.AsSpan(0, expectedSize);
                bytes.AsSpan(0, expectedSize).CopyTo(span);
                
                // 根据字节序调整
                ApplyEndianness<T>(span, endianness);
                
                var resultSpan = MemoryMarshal.Cast<byte, T>(span);
                return resultSpan[..count].ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(workingBuffer);
            }
        }
        
        /// <summary>
        /// 应用字节序转换
        /// </summary>
        private static void ApplyEndianness<T>(Span<byte> data, ModbusEndianness endianness) where T : unmanaged
        {
            var typeSize = Unsafe.SizeOf<T>();
            
            switch (endianness)
            {
                case ModbusEndianness.BigEndian:
                    // Modbus标准大端序：高字节在前，低字节在后
                    // 在小端序系统上不需要额外处理，保持原始字节顺序
                    // 因为MemoryMarshal.Cast已经按照系统字节序处理了数据
                    break;
                    
                case ModbusEndianness.LittleEndian:
                    // 小端序：在小端序系统上需要反转字节顺序
                    if (BitConverter.IsLittleEndian && typeSize > 1)
                    {
                        for (int i = 0; i < data.Length; i += typeSize)
                        {
                            var elementSpan = data.Slice(i, Math.Min(typeSize, data.Length - i));
                            if (elementSpan.Length >= 2)
                                elementSpan.Reverse();
                        }
                    }
                    break;
                    
                case ModbusEndianness.MidLittleEndian:
                    // 字内小端，字间大端
                    ApplyMidLittleEndian(data, typeSize);
                    break;
            }
        }
        
        /// <summary>
        /// 应用中端字节序转换
        /// </summary>
        private static void ApplyMidLittleEndian(Span<byte> data, int typeSize)
        {
            // 实现中端字节序转换逻辑
            for (int i = 0; i < data.Length; i += 4) // 假设以32位为单位
            {
                if (i + 3 < data.Length)
                {
                    // 交换字的顺序，但保持字内字节顺序
                    (data[i], data[i + 2]) = (data[i + 2], data[i]);
                    (data[i + 1], data[i + 3]) = (data[i + 3], data[i + 1]);
                }
            }
        }
    }
}