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
        /// <param name="byteOrder">字节序</param>
        /// <param name="wordOrder">字序</param>
        /// <returns>字节数组</returns>
        public static byte[] ToBytes<T>(T[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst)
            where T : unmanaged
        {
            ArgumentNullException.ThrowIfNull(values);

            var byteCount = Unsafe.SizeOf<T>() * values.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(byteCount);

            try
            {
                var span = buffer.AsSpan(0, byteCount);
                var sourceSpan = MemoryMarshal.Cast<T, byte>(values.AsSpan());
                sourceSpan.CopyTo(span);

                // 根据字节序和字序调整
                ApplyByteAndWordOrder<T>(span, byteOrder, wordOrder);

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
        /// <param name="byteOrder">字节序</param>
        /// <param name="wordOrder">字序</param>
        /// <returns>泛型数组</returns>
        public static T[] FromBytes<T>(byte[] bytes, int count, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst)
            where T : unmanaged
        {
            ArgumentNullException.ThrowIfNull(bytes);
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

                // 根据字节序和字序调整
                ApplyByteAndWordOrder<T>(span, byteOrder, wordOrder);

                var resultSpan = MemoryMarshal.Cast<byte, T>(span);
                return resultSpan[..count].ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(workingBuffer);
            }
        }
        
        /// <summary>
        /// 应用字节序和字序转换
        /// </summary>
        private static void ApplyByteAndWordOrder<T>(Span<byte> data, ByteOrder byteOrder, WordOrder wordOrder) where T : unmanaged
        {
            var typeSize = Unsafe.SizeOf<T>();

            // 根据Modbus工业标准，寄存器占两个字节且是大端序的
            // 先处理字节序（在寄存器内部）
            if (byteOrder == ByteOrder.LittleEndian && BitConverter.IsLittleEndian)
            {
                // 如果需要小端序，但系统是小端序，需要在每个寄存器（2字节）内部交换字节
                for (int i = 0; i < data.Length; i += 2)
                {
                    if (i + 1 < data.Length)
                    {
                        (data[i], data[i + 1]) = (data[i + 1], data[i]);
                    }
                }
            }

            // 再处理字序（寄存器之间）
            if (wordOrder == WordOrder.LowFirst && typeSize > 2)
            {
                // 如果需要低字在前，需要交换寄存器的顺序
                for (int i = 0; i < data.Length; i += typeSize)
                {
                    var elementSpan = data.Slice(i, Math.Min(typeSize, data.Length - i));
                    ReverseWordOrder(elementSpan);
                }
            }
        }

        /// <summary>
        /// 反转字序（以2字节为单位）
        /// </summary>
        private static void ReverseWordOrder(Span<byte> data)
        {
            var length = data.Length;
            for (int i = 0; i < length / 2; i += 2)
            {
                var j = length - i - 2;
                if (j >= i + 2)
                {
                    // 交换两个字节作为一个整体
                    (data[i], data[j]) = (data[j], data[i]);
                    (data[i + 1], data[j + 1]) = (data[j + 1], data[i + 1]);
                }
            }
        }
    }
}