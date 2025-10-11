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
        public static byte[] ToBytes<T>(T[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst)
            where T : unmanaged {
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
            where T : unmanaged {
            ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));
            if (count < 0)
                throw new ArgumentException("元素数量不能为负数", nameof(count));

            var expectedSize = Unsafe.SizeOf<T>() * count;
            if (bytes.Length < expectedSize)
                throw new ArgumentException($"字节数组长度不足，需要 {expectedSize} 字节，实际 {bytes.Length} 字节");

            var workingBuffer = ArrayPool<byte>.Shared.Rent(expectedSize);
            try {
                var span = workingBuffer.AsSpan(0, expectedSize);
                bytes.AsSpan(0, expectedSize).CopyTo(span);

                // 根据字节序和字序调整
                ApplyByteAndWordOrder<T>(span, byteOrder, wordOrder);

                var resultSpan = MemoryMarshal.Cast<byte, T>(span);
                return resultSpan[..count].ToArray();
            } finally {
                ArrayPool<byte>.Shared.Return(workingBuffer);
            }
        }

        /// <summary>
        /// 应用字节序和字序转换
        /// </summary>
        private static void ApplyByteAndWordOrder<T>(Span<byte> data, ByteOrder byteOrder, WordOrder wordOrder) where T : unmanaged {
            var typeSize = Unsafe.SizeOf<T>();

            // 先处理字节序
            // 需要转换的条件：数据存储字节序和系统字节序不一致
            if (byteOrder.IsLittleEndian() != BitConverter.IsLittleEndian) {
                for (int i = 0; i < data.Length; i += 2) {
                    if (i + 1 < data.Length) {
                        (data[i], data[i + 1]) = (data[i + 1], data[i]);
                    }
                }
            }

            // 再处理字序
            if (wordOrder.IsLowFirst() != BitConverter.IsLittleEndian && typeSize >= 4) {
                // 按typeSize分组，对每组内的Word进行倒序重排
                int count = data.Length / typeSize;
                for (int index = 0; index < count; index++) {
                    int start = index * typeSize;
                    Span<byte> element = data.Slice(start, typeSize);

                    // 对组内的Word进行倒序重排
                    int wordCount = typeSize / 2;
                    for (int i = 0; i < wordCount / 2; i++) {
                        int left = i * 2;
                        int right = (wordCount - 1 - i) * 2;

                        // 交换两个Word（各2个字节）
                        (element[left], element[right]) = (element[right], element[left]);
                        (element[left + 1], element[right + 1]) = (element[right + 1], element[left + 1]);
                    }
                }
            }
        }

        /// <summary>
        /// 反转字序（以2字节为单位）
        /// </summary>
        private static void ReverseWordOrder(Span<byte> data) {
            var length = data.Length;
            for (int i = 0; i < length / 2; i += 2) {
                var j = length - i - 2;
                if (j >= i + 2) {
                    // 交换两个字节作为一个整体
                    (data[i], data[j]) = (data[j], data[i]);
                    (data[i + 1], data[j + 1]) = (data[j + 1], data[i + 1]);
                }
            }
        }

        private static T[] Convert<T>(byte[] bytes, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
            ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));
            var t_size = Unsafe.SizeOf<T>();
            var count = bytes.Length / t_size;
            if (count <= 0) throw new ArgumentException("可转换元素数量不能小于 1");
            if (count * t_size != bytes.Length) {
                throw new ArgumentException("原始字节数量不是转换数量的整数倍");
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
    }
}
