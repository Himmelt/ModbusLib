using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Utils;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus客户端基类
/// </summary>
public abstract class ModbusClientBase : IModbusClient {

    private bool _disposed;

    /// <summary>
    /// 获取一个值，表示当前对象是否已被释放
    /// </summary>
    protected bool IsDisposed => _disposed;

    public int Retries { get; set; }

    public int Timeout {
        get => Transport.Timeout;
        set => Transport.Timeout = value;
    }

    public bool IsConnected => Transport.IsConnected;
    /// <summary>
    /// 获取Modbus协议实现
    /// </summary>
    protected abstract IModbusProtocol Protocol { get; set; }

    /// <summary>
    /// 获取Modbus传输实现
    /// </summary>
    protected abstract IModbusTransport Transport { get; set; }

    public virtual async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await Transport.ConnectAsync(cancelToken).ConfigureAwait(false);
    }

    public virtual async Task DisconnectAsync(CancellationToken cancelToken = default) {
        if (_disposed) return;
        await Transport.DisconnectAsync(cancelToken).ConfigureAwait(false);
    }

    #region 读取功能

    public async Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancelToken = default) {
        ValidateReadParameters(quantity, 2000);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadCoils, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);
        var dataBytes = ExtractReadData(response, ModbusFunction.ReadCoils, expectedByteCount: -1);

        return DataConverter.ByteArrayToBoolArray(dataBytes, quantity);
    }

    public async Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancelToken = default) {
        ValidateReadParameters(quantity, 2000);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadDiscreteInputs, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);
        var dataBytes = ExtractReadData(response, ModbusFunction.ReadDiscreteInputs, expectedByteCount: -1);

        return DataConverter.ByteArrayToBoolArray(dataBytes, quantity);
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort quantity, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) {
        var dataBytes = await ReadHoldingRegistersRawAsync(unitId, startAddress, quantity, cancelToken).ConfigureAwait(false);
        return DataConverter.Convert<ushort>(dataBytes, byteOrder, wordOrder);
    }

    public async Task<byte[]> ReadHoldingRegistersRawAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancelToken = default) {
        ValidateReadParameters(quantity, 125);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadHoldingRegisters, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);
        return ExtractReadData(response, ModbusFunction.ReadHoldingRegisters, quantity * 2);
    }

    public async Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort quantity, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) {
        var dataBytes = await ReadInputRegistersRawAsync(unitId, startAddress, quantity, cancelToken).ConfigureAwait(false);
        return DataConverter.Convert<ushort>(dataBytes, byteOrder, wordOrder);
    }

    public async Task<byte[]> ReadInputRegistersRawAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancelToken = default) {
        ValidateReadParameters(quantity, 125);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadInputRegisters, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);
        return ExtractReadData(response, ModbusFunction.ReadInputRegisters, quantity * 2);
    }

    #endregion

    #region 泛型读取功能

    public async Task<T[]> ReadHoldingRegistersAsync<T>(byte unitId, ushort startAddress, ushort count, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) where T : unmanaged {
        if (count == 0) throw new ArgumentException("元素数量不能为 0", nameof(count));

        var registerCount = (ushort)DataConverter.GetTotalRegisterCount<T>(count);
        ValidateReadParameters(registerCount, 125);

        var rawBytes = await ReadHoldingRegistersRawAsync(unitId, startAddress, registerCount, cancelToken).ConfigureAwait(false);
        // 验证rawBytes长度是否匹配
        if (rawBytes.Length < registerCount * 2) {
            throw new ModbusCommunicationException($"响应数据长度 {rawBytes.Length} 不满足转换 {count} 个目标数据类型 {typeof(T)}");
        }
        // 使用泛型转换器转换为目标类型
        var result = DataConverter.Convert<T>(rawBytes, byteOrder, wordOrder);
        // 对于 byte/sbyte 等奇数大小类型，读回的字节数可能多于请求数量，截断到请求数量
        return result.Length == count ? result : result[..count];
    }

    public async Task<T[]> ReadInputRegistersAsync<T>(byte unitId, ushort startAddress, ushort count, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) where T : unmanaged {
        if (count == 0) throw new ArgumentException("元素数量不能为 0", nameof(count));

        var registerCount = (ushort)DataConverter.GetTotalRegisterCount<T>(count);
        ValidateReadParameters(registerCount, 125);

        var rawBytes = await ReadInputRegistersRawAsync(unitId, startAddress, registerCount, cancelToken).ConfigureAwait(false);
        // 验证rawBytes长度是否匹配
        if (rawBytes.Length < registerCount * 2) {
            throw new ModbusCommunicationException($"响应数据长度 {rawBytes.Length} 不满足转换 {count} 个目标数据类型 {typeof(T)}");
        }
        // 使用泛型转换器转换为目标类型
        var result = DataConverter.Convert<T>(rawBytes, byteOrder, wordOrder);
        // 对于 byte/sbyte 等奇数大小类型，读回的字节数可能多于请求数量，截断到请求数量
        return result.Length == count ? result : result[..count];
    }

    #endregion

    #region 写入功能

    public async Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken cancelToken = default) {
        var data = new byte[] { (byte)(value ? 1 : 0) };
        var request = new ModbusRequest(unitId, ModbusFunction.WriteSingleCoil, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteSingleCoil);
    }

    public async Task WriteMultipleCoilsAsync(byte unitId, ushort startAddress, bool[] values, CancellationToken cancelToken = default) {
        if (values == null || values.Length == 0) {
            throw new ArgumentException("线圈值数组不能为空", nameof(values));
        }

        if (values.Length > 1968) {
            throw new ArgumentException("写入线圈数量不能超过1968", nameof(values));
        }

        var data = DataConverter.BoolArrayToByteArray(values);
        var request = new ModbusRequest(unitId, ModbusFunction.WriteMultipleCoils, startAddress, (ushort)values.Length, data);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);

        if (response.IsError) {
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteMultipleCoils);
        }
    }

    public async Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) {
        var data = DataConverter.Convert([value], byteOrder, wordOrder);
        var request = new ModbusRequest(unitId, ModbusFunction.WriteSingleRegister, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);

        if (response.IsError) {
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteSingleRegister);
        }
    }

    public async Task WriteSingleRegisterAsync(byte unitId, ushort address, short value, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) {
        var data = DataConverter.Convert([value], byteOrder, wordOrder);
        var request = new ModbusRequest(unitId, ModbusFunction.WriteSingleRegister, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);

        if (response.IsError) {
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteSingleRegister);
        }
    }

    public async Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) {
        if (values == null || values.Length == 0) {
            throw new ArgumentException("寄存器值数组不能为空", nameof(values));
        }

        if (values.Length > 123) {
            throw new ArgumentException("写入寄存器数量不能超过123", nameof(values));
        }

        var data = DataConverter.Convert(values, byteOrder, wordOrder);
        var request = new ModbusRequest(unitId, ModbusFunction.WriteMultipleRegisters, startAddress, (ushort)values.Length, data);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);

        if (response.IsError) {
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteMultipleRegisters);
        }
    }

    public async Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, short[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) {
        if (values == null || values.Length == 0) {
            throw new ArgumentException("寄存器值数组不能为空", nameof(values));
        }

        if (values.Length > 123) {
            throw new ArgumentException("写入寄存器数量不能超过123", nameof(values));
        }

        var data = DataConverter.Convert(values, byteOrder, wordOrder);
        var request = new ModbusRequest(unitId, ModbusFunction.WriteMultipleRegisters, startAddress, (ushort)values.Length, data);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);

        if (response.IsError) {
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteMultipleRegisters);
        }
    }

    public async Task WriteMultipleRegistersRawAsync(byte unitId, ushort startAddress, byte[] rawBytes, CancellationToken cancelToken = default) {
        if (rawBytes == null || rawBytes.Length == 0) {
            throw new ArgumentException("原始字节数组不能为空", nameof(rawBytes));
        }

        if (rawBytes.Length > 246) {
            throw new ArgumentException("写入寄存器字节数量不能超过246（123个寄存器）", nameof(rawBytes));
        }

        if (rawBytes.Length % 2 != 0) {
            throw new ArgumentException("寄存器数据必须为偶数个字节（每个寄存器占2字节）", nameof(rawBytes));
        }

        var request = new ModbusRequest(unitId, ModbusFunction.WriteMultipleRegisters, startAddress, (ushort)(rawBytes.Length / 2), rawBytes);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);

        if (response.IsError) {
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteMultipleRegisters);
        }
    }

    #endregion

    #region 泛型写入功能

    public async Task WriteMultipleRegistersAsync<T>(byte unitId, ushort startAddress, T value, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) where T : unmanaged {
        var registerCount = DataConverter.GetRegisterCount<T>();
        if (registerCount > 4) {
            throw new ArgumentException($"写入所需寄存器数量({registerCount})不能超过4", nameof(value));
        }

        // 将泛型转换为字节数组
        var rawBytes = DataConverter.Convert([value], byteOrder, wordOrder);
        // byte/sbyte 等奇数大小类型会产生奇数字节，末尾补0凑成完整寄存器
        if (rawBytes.Length % 2 != 0) {
            Array.Resize(ref rawBytes, rawBytes.Length + 1);
        }

        // 调用原始写入方法
        await WriteMultipleRegistersRawAsync(unitId, startAddress, rawBytes, cancelToken).ConfigureAwait(false);
    }

    public async Task WriteMultipleRegistersAsync<T>(byte unitId, ushort startAddress, T[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) where T : unmanaged {
        if (values == null || values.Length == 0) {
            throw new ArgumentException("值数组不能为空", nameof(values));
        }

        var registerCount = DataConverter.GetTotalRegisterCount<T>(values.Length);
        if (registerCount > 123) {
            throw new ArgumentException($"写入所需寄存器数量({registerCount})不能超过123", nameof(values));
        }

        // 将泛型数组转换为字节数组
        var rawBytes = DataConverter.Convert(values, byteOrder, wordOrder);
        // byte/sbyte 等奇数大小类型会产生奇数字节，末尾补0凑成完整寄存器
        if (rawBytes.Length % 2 != 0) {
            Array.Resize(ref rawBytes, rawBytes.Length + 1);
        }

        // 调用原始写入方法
        await WriteMultipleRegistersRawAsync(unitId, startAddress, rawBytes, cancelToken).ConfigureAwait(false);
    }

    #endregion

    #region 高级功能

    public async Task<ushort[]> ReadWriteMultipleRegistersAsync(byte unitId, ushort readStartAddress, ushort readQuantity, ushort writeStartAddress, ushort[] writeValues, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancelToken = default) {
        ValidateReadParameters(readQuantity, 125);

        if (writeValues == null || writeValues.Length == 0) {
            throw new ArgumentException("写入寄存器值数组不能为空", nameof(writeValues));
        }

        if (writeValues.Length > 121) {
            throw new ArgumentException("写入寄存器数量不能超过121", nameof(writeValues));
        }

        var writeData = DataConverter.Convert(writeValues, byteOrder, wordOrder);
        var requestData = new byte[4 + writeData.Length];

        // 写入起始地址
        requestData[0] = (byte)(writeStartAddress >> 8);
        requestData[1] = (byte)(writeStartAddress & 0xFF);
        // 写入数量
        requestData[2] = (byte)(writeValues.Length >> 8);
        requestData[3] = (byte)(writeValues.Length & 0xFF);
        // 写入数据
        Array.Copy(writeData, 0, requestData, 4, writeData.Length);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadWriteMultipleRegisters, readStartAddress, readQuantity, requestData);
        var response = await ExecuteRequestAsync(request, cancelToken).ConfigureAwait(false);
        var dataBytes = ExtractReadData(response, ModbusFunction.ReadWriteMultipleRegisters, readQuantity * 2);

        return DataConverter.Convert<ushort>(dataBytes, byteOrder, wordOrder);
    }

    #endregion

    #region 同步连接管理

    public virtual bool Connect() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public virtual void Disconnect() {
        if (_disposed)
            return;

        DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步读取功能

    public bool[] ReadCoils(byte unitId, ushort startAddress, ushort quantity) {
        return ReadCoilsAsync(unitId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    public bool[] ReadDiscreteInputs(byte unitId, ushort startAddress, ushort quantity) {
        return ReadDiscreteInputsAsync(unitId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    public ushort[] ReadHoldingRegisters(byte unitId, ushort startAddress, ushort quantity, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) {
        return ReadHoldingRegistersAsync(unitId, startAddress, quantity, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public byte[] ReadHoldingRegistersRaw(byte unitId, ushort startAddress, ushort quantity) {
        return ReadHoldingRegistersRawAsync(unitId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    public ushort[] ReadInputRegisters(byte unitId, ushort startAddress, ushort quantity, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) {
        return ReadInputRegistersAsync(unitId, startAddress, quantity, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public byte[] ReadInputRegistersRaw(byte unitId, ushort startAddress, ushort quantity) {
        return ReadInputRegistersRawAsync(unitId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步写入功能

    public void WriteSingleCoil(byte unitId, ushort address, bool value) {
        WriteSingleCoilAsync(unitId, address, value, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleCoils(byte unitId, ushort startAddress, bool[] values) {
        WriteMultipleCoilsAsync(unitId, startAddress, values, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteSingleRegister(byte unitId, ushort address, ushort value, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) {
        WriteSingleRegisterAsync(unitId, address, value, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteSingleRegister(byte unitId, ushort address, short value, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) {
        WriteSingleRegisterAsync(unitId, address, value, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleRegisters(byte unitId, ushort startAddress, ushort[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) {
        WriteMultipleRegistersAsync(unitId, startAddress, values, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleRegisters(byte unitId, ushort startAddress, short[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) {
        WriteMultipleRegistersAsync(unitId, startAddress, values, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步高级功能

    public ushort[] ReadWriteMultipleRegisters(byte unitId, ushort readStartAddress, ushort readQuantity, ushort writeStartAddress, ushort[] writeValues, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) {
        return ReadWriteMultipleRegistersAsync(unitId, readStartAddress, readQuantity, writeStartAddress, writeValues, byteOrder, wordOrder, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    #endregion

    #region 同步泛型读取功能

    public T[] ReadHoldingRegisters<T>(byte unitId, ushort startAddress, ushort count, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        return ReadHoldingRegistersAsync<T>(unitId, startAddress, count, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public T[] ReadInputRegisters<T>(byte unitId, ushort startAddress, ushort count, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        return ReadInputRegistersAsync<T>(unitId, startAddress, count, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步泛型写入功能

    public void WriteMultipleRegisters<T>(byte unitId, ushort startAddress, T value, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        WriteMultipleRegistersAsync(unitId, startAddress, value, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleRegisters<T>(byte unitId, ushort startAddress, T[] values, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        WriteMultipleRegistersAsync(unitId, startAddress, values, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    protected async Task<ModbusResponse> ExecuteRequestAsync(ModbusRequest request, CancellationToken cancelToken) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) throw new ModbusConnectionException("客户端未连接");

        Exception? lastException = null;

        for (int attempt = 0; attempt <= Retries; attempt++) {
            // 检查取消令牌
            cancelToken.ThrowIfCancelRequestCN();

            try {
                var requestBytes = Protocol.BuildRequest(request);
                var responseBytes = await Transport.SendReceiveAsync(requestBytes, cancelToken).ConfigureAwait(false);

                if (!Protocol.ValidateResponse(responseBytes)) {
                    throw new ModbusCommunicationException($"响应数据验证失败: 响应长度 = {responseBytes?.Length ?? 0}");
                }

                return Protocol.ParseResponse(responseBytes, request);
            } catch (OperationCanceledException) {
                throw; // 保留用户取消语义
            } catch (Exception ex) when (attempt < Retries && IsRetryableException(ex)) {
                lastException = ex;
                // 通信层异常时先尝试断开并重连，让重试真正具备自愈能力
                await TryReconnectAsync(cancelToken).ConfigureAwait(false);
                // 在重试前检查取消令牌
                cancelToken.ThrowIfCancelRequestCN();
                await Task.Delay(100 * (attempt + 1), cancelToken).ConfigureAwait(false); // 递增延迟
            }
        }

        throw lastException ?? new ModbusCommunicationException("请求执行失败");
    }

    private static bool IsRetryableException(Exception exception) {
        return exception is ModbusTimeoutException ||
               exception is ModbusCommunicationException ||
               exception is ModbusConnectionException ||
               (exception is ModbusException modbusEx &&
                modbusEx.ExceptionCode == ModbusExceptionCode.TargetDeviceBusy);
    }

    private async Task TryReconnectAsync(CancellationToken cancelToken) {
        try {
            await Transport.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            await Transport.ConnectAsync(cancelToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) when (ex is ModbusConnectionException or ModbusTimeoutException or ModbusCommunicationException) {
            // 重连失败：由下一次请求尝试抛出真实的连接异常
        }
    }

    private static byte[] ExtractReadData(ModbusResponse response, ModbusFunction function, int expectedByteCount) {
        if (response.IsError) {
            throw new ModbusException(response.ExceptionCode!.Value, response.UnitId, function);
        }

        if (response.Data.IsEmpty || response.Data.Length < 1) {
            throw new ModbusCommunicationException("读取响应数据不足");
        }

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || (expectedByteCount >= 0 && byteCount != expectedByteCount)) {
            throw new ModbusCommunicationException("读取响应数据长度不匹配");
        }

        var dataBytes = new byte[byteCount];
        response.Data.Slice(1, byteCount).CopyTo(dataBytes);
        return dataBytes;
    }

    private static void ValidateReadParameters(int quantity, int maxQuantity) {
        if (quantity == 0) throw new ArgumentException("读取数量不能为 0", nameof(quantity));
        if (quantity > maxQuantity) throw new ArgumentException($"读取数量不能超过 {maxQuantity}", nameof(quantity));
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed) return;

        if (disposing) {
            Transport?.Dispose();
        }
        _disposed = true;
    }

    public async ValueTask DisposeAsync() {
        if (_disposed) return;

        if (Transport != null) {
            await Transport.DisposeAsync().ConfigureAwait(false);
        }
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
