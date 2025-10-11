using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus客户端基类
/// </summary>
public abstract class ModbusClientBase(IModbusTransport transport, IModbusProtocol protocol) : IModbusClient {

    private bool _disposed;
    private readonly IModbusProtocol _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
    private readonly IModbusTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <summary>
    /// 获取一个值，表示当前对象是否已被释放
    /// </summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// 获取Modbus协议实现
    /// </summary>
    protected IModbusProtocol Protocol => _protocol;

    /// <summary>
    /// 获取Modbus传输实现
    /// </summary>
    protected IModbusTransport Transport => _transport;

    public TimeSpan Timeout {
        get => _transport.Timeout;
        set => _transport.Timeout = value;
    }

    public int Retries { get; set; } = 3;

    public bool IsConnected => Transport.IsConnected;

    public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if (_disposed)
            return;

        await _transport.DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    #region 读取功能

    public async Task<bool[]> ReadCoilsAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 2000);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadCoils, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.ReadCoils);

        if (response.Data.IsEmpty || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取线圈响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount)
            throw new ModbusCommunicationException("读取线圈响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data.ToArray(), 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToBoolArray(dataBytes, quantity);
    }

    public async Task<bool[]> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 2000);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadDiscreteInputs, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.ReadDiscreteInputs);

        if (response.Data.IsEmpty || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取离散输入响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount)
            throw new ModbusCommunicationException("读取离散输入响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data.ToArray(), 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToBoolArray(dataBytes, quantity);
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 125);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadHoldingRegisters, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.ReadHoldingRegisters);

        if (response.Data.IsEmpty || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取保持寄存器响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || byteCount != quantity * 2)
            throw new ModbusCommunicationException("读取保持寄存器响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data.ToArray(), 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToUshortArray(dataBytes);
    }

    public async Task<byte[]> ReadHoldingRegistersRawAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 125);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadHoldingRegisters, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.ReadHoldingRegisters);

        if (response.Data.IsEmpty || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取保持寄存器响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || byteCount != quantity * 2)
            throw new ModbusCommunicationException("读取保持寄存器响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data.ToArray(), 1, dataBytes, 0, byteCount);

        return dataBytes;
    }

    public async Task<ushort[]> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 125);

        var request = new ModbusRequest(unitId, ModbusFunction.ReadInputRegisters, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.ReadInputRegisters);

        if (response.Data.IsEmpty || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取输入寄存器响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || byteCount != quantity * 2)
            throw new ModbusCommunicationException("读取输入寄存器响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data.ToArray(), 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToUshortArray(dataBytes);
    }

    #endregion

    #region 泛型读取功能

    public async Task<T[]> ReadHoldingRegistersAsync<T>(byte unitId, ushort startAddress, ushort count, ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancellationToken = default) where T : unmanaged {
        if (count == 0) throw new ArgumentException("元素数量不能为 0", nameof(count));

        var registerCount = (ushort)ModbusDataConverter.GetTotalRegisterCount<T>(count);
        ValidateReadParameters(registerCount, 125);

        var rawBytes = await ReadHoldingRegistersRawAsync(unitId, startAddress, registerCount, cancellationToken).ConfigureAwait(false);

        // 使用泛型转换器转换为目标类型
        return ModbusDataConverter.FromBytes<T>(rawBytes, count, byteOrder, wordOrder);
    }

    public async Task<T[]> ReadInputRegistersAsync<T>(byte unitId, ushort startAddress, ushort count,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancellationToken = default) where T : unmanaged {
        if (count == 0)
            throw new ArgumentException("元素数量不能为0", nameof(count));

        var registerCount = (ushort)ModbusDataConverter.GetTotalRegisterCount<T>(count);
        ValidateReadParameters(registerCount, 125);

        var registers = await ReadInputRegistersAsync(unitId, startAddress, registerCount, cancellationToken).ConfigureAwait(false);

        // 将寄存器数据转换为字节数组
        var bytes = ModbusUtils.UshortArrayToByteArray(registers);

        // 使用泛型转换器转换为目标类型
        return ModbusDataConverter.FromBytes<T>(bytes, count, byteOrder, wordOrder);
    }

    #endregion

    #region 写入功能

    public async Task WriteSingleCoilAsync(byte unitId, ushort address, bool value, CancellationToken cancellationToken = default) {
        var data = new byte[] { (byte)(value ? 1 : 0) };
        var request = new ModbusRequest(unitId, ModbusFunction.WriteSingleCoil, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteSingleCoil);
    }

    public async Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken cancellationToken = default) {
        var data = new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        var request = new ModbusRequest(unitId, ModbusFunction.WriteSingleRegister, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteSingleRegister);
    }

    public async Task WriteSingleRegisterAsync(byte unitId, ushort address, short value, CancellationToken cancellationToken = default) {
        var data = new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        var request = new ModbusRequest(unitId, ModbusFunction.WriteSingleRegister, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteSingleRegister);
    }

    public async Task WriteMultipleCoilsAsync(byte unitId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default) {
        if (values == null || values.Length == 0)
            throw new ArgumentException("线圈值数组不能为空", nameof(values));

        if (values.Length > 1968)
            throw new ArgumentException("线圈数量不能超过1968", nameof(values));

        var data = ModbusUtils.BoolArrayToByteArray(values);
        var request = new ModbusRequest(unitId, ModbusFunction.WriteMultipleCoils, startAddress, (ushort)values.Length, data);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteMultipleCoils);
    }

    public async Task WriteMultipleRegistersAsync(byte unitId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default) {
        if (values == null || values.Length == 0)
            throw new ArgumentException("寄存器值数组不能为空", nameof(values));

        if (values.Length > 123)
            throw new ArgumentException("寄存器数量不能超过123", nameof(values));

        var data = ModbusUtils.UshortArrayToByteArray(values);
        var request = new ModbusRequest(unitId, ModbusFunction.WriteMultipleRegisters, startAddress, (ushort)values.Length, data);
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.WriteMultipleRegisters);
    }

    #endregion

    #region 泛型写入功能

    public async Task WriteMultipleRegistersAsync<T>(byte unitId, ushort startAddress, T value,
    ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancellationToken = default) where T : unmanaged {
        var registerCount = ModbusDataConverter.GetRegisterCount<T>();
        if (registerCount > 4)
            throw new ArgumentException($"所需寄存器数量({registerCount})不能超过4", nameof(value));

        // 将泛型转换为字节数组
        var bytes = ModbusDataConverter.ToBytes([value], byteOrder, wordOrder);

        // 将字节数组转换为寄存器数组
        var registers = ModbusUtils.ByteArrayToUshortArray(bytes);

        // 调用原始写入方法
        await WriteMultipleRegistersAsync(unitId, startAddress, registers, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteMultipleRegistersAsync<T>(byte unitId, ushort startAddress, T[] values,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancellationToken = default) where T : unmanaged {
        if (values == null || values.Length == 0)
            throw new ArgumentException("值数组不能为空", nameof(values));

        var registerCount = ModbusDataConverter.GetTotalRegisterCount<T>(values.Length);
        if (registerCount > 123)
            throw new ArgumentException($"所需寄存器数量({registerCount})不能超过123", nameof(values));

        // 将泛型数组转换为字节数组
        var bytes = ModbusDataConverter.ToBytes(values, byteOrder, wordOrder);

        // 将字节数组转换为寄存器数组
        var registers = ModbusUtils.ByteArrayToUshortArray(bytes);

        // 调用原始写入方法
        await WriteMultipleRegistersAsync(unitId, startAddress, registers, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region 高级功能

    public async Task<ushort[]> ReadWriteMultipleRegistersAsync(byte unitId, ushort readStartAddress, ushort readQuantity,
        ushort writeStartAddress, ushort[] writeValues, CancellationToken cancellationToken = default) {
        ValidateReadParameters(readQuantity, 125);

        if (writeValues == null || writeValues.Length == 0)
            throw new ArgumentException("写入寄存器值数组不能为空", nameof(writeValues));

        if (writeValues.Length > 121)
            throw new ArgumentException("写入寄存器数量不能超过121", nameof(writeValues));

        var writeData = ModbusUtils.UshortArrayToByteArray(writeValues);
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
        var response = await ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, unitId, ModbusFunction.ReadWriteMultipleRegisters);

        if (response.Data.IsEmpty || response.Data.Length < 1)
            throw new ModbusCommunicationException("读写多个寄存器响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || byteCount != readQuantity * 2)
            throw new ModbusCommunicationException("读写多个寄存器响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data.ToArray(), 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToUshortArray(dataBytes);
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

    public ushort[] ReadHoldingRegisters(byte unitId, ushort startAddress, ushort quantity) {
        return ReadHoldingRegistersAsync(unitId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    public ushort[] ReadInputRegisters(byte unitId, ushort startAddress, ushort quantity) {
        return ReadInputRegistersAsync(unitId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步写入功能

    public void WriteSingleCoil(byte unitId, ushort address, bool value) {
        WriteSingleCoilAsync(unitId, address, value, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteSingleRegister(byte unitId, ushort address, ushort value) {
        WriteSingleRegisterAsync(unitId, address, value, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteSingleRegister(byte unitId, ushort address, short value) {
        WriteSingleRegisterAsync(unitId, address, value, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleCoils(byte unitId, ushort startAddress, bool[] values) {
        WriteMultipleCoilsAsync(unitId, startAddress, values, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleRegisters(byte unitId, ushort startAddress, ushort[] values) {
        WriteMultipleRegistersAsync(unitId, startAddress, values, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步高级功能

    public ushort[] ReadWriteMultipleRegisters(byte unitId, ushort readStartAddress, ushort readQuantity,
        ushort writeStartAddress, ushort[] writeValues) {
        return ReadWriteMultipleRegistersAsync(unitId, readStartAddress, readQuantity, writeStartAddress, writeValues, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    #endregion

    #region 同步泛型读取功能

    public T[] ReadHoldingRegisters<T>(byte unitId, ushort startAddress, ushort count,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        return ReadHoldingRegistersAsync<T>(unitId, startAddress, count, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public T[] ReadInputRegisters<T>(byte unitId, ushort startAddress, ushort count,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        return ReadInputRegistersAsync<T>(unitId, startAddress, count, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步泛型写入功能

    public void WriteMultipleRegisters<T>(byte unitId, ushort startAddress, T[] values,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        WriteMultipleRegistersAsync<T>(unitId, startAddress, values, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    protected async Task<ModbusResponse> ExecuteRequestAsync(ModbusRequest request, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected)
            throw new ModbusConnectionException("客户端未连接");

        Exception? lastException = null;

        for (int attempt = 0; attempt <= Retries; attempt++) {
            // 检查取消令牌
            cancellationToken.ThrowIfCancellationRequested();

            try {
                var requestBytes = _protocol.BuildRequest(request);
                var responseBytes = await _transport.SendReceiveAsync(requestBytes, cancellationToken).ConfigureAwait(false);

                if (!_protocol.ValidateResponse(responseBytes))
                    throw new ModbusCommunicationException($"响应数据验证失败: response length = {responseBytes?.Length ?? 0}");

                return _protocol.ParseResponse(responseBytes, request);
            } catch (Exception ex) when (attempt < Retries && IsRetryableException(ex)) {
                lastException = ex;
                // 在重试前检查取消令牌
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100 * (attempt + 1), cancellationToken).ConfigureAwait(false); // 递增延迟
            }
        }

        throw lastException ?? new ModbusCommunicationException("请求执行失败");
    }

    private static bool IsRetryableException(Exception exception) {
        return exception is ModbusTimeoutException ||
               exception is ModbusCommunicationException ||
               (exception is ModbusException modbusEx &&
                modbusEx.ExceptionCode == ModbusExceptionCode.TargetDeviceBusy);
    }

    private static void ValidateReadParameters(int quantity, int maxQuantity) {
        if (quantity == 0) throw new ArgumentException("数量不能为0", nameof(quantity));
        if (quantity > maxQuantity) throw new ArgumentException($"数量不能超过{maxQuantity}", nameof(quantity));
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        _disposed = true;

        if (disposing) {
            _transport?.Dispose();
        }
    }

    public async ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore() {
        if (_disposed)
            return;

        _disposed = true;

        if (_transport != null) {
            try {
                await DisconnectAsync().ConfigureAwait(false);
            } catch {
                // 忽略断开连接时的异常
            }

            // 优先使用异步释放，回退到同步释放
            if (_transport is IAsyncDisposable asyncDisposableTransport) {
                await asyncDisposableTransport.DisposeAsync().ConfigureAwait(false);
            } else {
                _transport.Dispose();
            }
        }
    }
}