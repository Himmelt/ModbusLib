using ModbusLib.Enums;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModbusLib.Models {
    /// <summary>
    /// Modbus数据类型转换器
    /// </summary>
    public static class ModbusDataConverter {
        /// <summary>
        /// 计算指定类型需要的寄存器数量
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>寄存器数量</returns>
        public static int GetRegisterCount<T>() where T : unmanaged {
            return (Unsafe.SizeOf<T>() + 1) / 2; // 向上取整
        }

        /// <summary>
        /// 计算指定数量元素需要的寄存器数量
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="count">T类型元素数量</param>
        /// <returns>所需的寄存器数量 (例如: 10个byte需要5个寄存器，5个int需要10个寄存器)</returns>
        public static int GetTotalRegisterCount<T>(int count) where T : unmanaged {
            if (count <= 0) return 0;
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
        public static byte[] Convert<T>(T[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
            ArgumentNullException.ThrowIfNull(values, nameof(values));

            var byteCount = Unsafe.SizeOf<T>() * values.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(byteCount);

            try {
                var span = buffer.AsSpan(0, byteCount);
                var sourceSpan = MemoryMarshal.Cast<T, byte>(values.AsSpan());
                sourceSpan.CopyTo(span);

                // 根据字节序和字序调整
                ApplyByteAndWordOrder<T>(span, byteOrder, wordOrder);

                return span.ToArray();
            } finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static T[] Convert<T>(byte[] bytes, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
            ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));

            var t_size = Unsafe.SizeOf<T>();
            var count = bytes.Length / t_size;

            if (count <= 0) {
                throw new ArgumentException("可转换元素数量必须大于 0", nameof(bytes));
            }
            if (count * t_size != bytes.Length) {
                throw new ArgumentException($"字节数组长度 ({bytes.Length}) 必须是类型 {typeof(T).Name} 大小 ({t_size} 字节) 的整数倍", nameof(bytes));
            }

            var buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
            try {
                var buff_span = buffer.AsSpan(0, bytes.Length);
                bytes.AsSpan().CopyTo(buff_span);

                // 根据字节序和字序调整
                ApplyByteAndWordOrder<T>(buff_span, byteOrder, wordOrder);

                var results = MemoryMarshal.Cast<byte, T>(buff_span);
                return results[..count].ToArray();
            } finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 应用字节序和字序转换
        /// </summary>
        private static void ApplyByteAndWordOrder<T>(Span<byte> bytes, ByteOrder byteOrder, WordOrder wordOrder) where T : unmanaged {
            var typeSize = Unsafe.SizeOf<T>();

            // 先处理字节序
            // 需要转换的条件：数据存储字节序和系统字节序不一致
            if (byteOrder.IsLittleEndian() != BitConverter.IsLittleEndian) {
                for (int i = 0; i < bytes.Length; i += 2) {
                    if (i + 1 < bytes.Length) {
                        (bytes[i], bytes[i + 1]) = (bytes[i + 1], bytes[i]);
                    }
                }
            }

            // 再处理字序
            if (wordOrder.IsLowFirst() != BitConverter.IsLittleEndian && typeSize >= 4) {
                // 按typeSize分组，对每组内的Word进行倒序重排
                int count = bytes.Length / typeSize;
                for (int index = 0; index < count; index++) {
                    int start = index * typeSize;
                    Span<byte> data = bytes.Slice(start, typeSize);

                    // 对组内的Word进行倒序重排
                    int wordCount = typeSize / 2;
                    for (int i = 0; i < wordCount / 2; i++) {
                        int left = i * 2;
                        int right = (wordCount - 1 - i) * 2;

                        // 交换两个Word（各2个字节）
                        (data[left], data[right]) = (data[right], data[left]);
                        (data[left + 1], data[right + 1]) = (data[right + 1], data[left + 1]);
                    }
                }
            }
        }
    }
}
