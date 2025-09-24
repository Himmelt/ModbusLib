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

    protected bool _disposed = false;
    protected readonly IModbusProtocol _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
    protected readonly IModbusTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    public TimeSpan Timeout {
        get => _transport.Timeout;
        set => _transport.Timeout = value;
    }

    public int Retries { get; set; } = 3;

    public bool IsConnected => _transport.IsConnected;

    public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await _transport.ConnectAsync(cancellationToken);
    }

    public virtual async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if (_disposed)
            return;

        await _transport.DisconnectAsync(cancellationToken);
    }

    #region 读取功能

    public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 2000);

        var request = new ModbusRequest(slaveId, ModbusFunction.ReadCoils, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.ReadCoils);

        if (response.Data == null || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取线圈响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount)
            throw new ModbusCommunicationException("读取线圈响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data, 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToBoolArray(dataBytes, quantity);
    }

    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 2000);

        var request = new ModbusRequest(slaveId, ModbusFunction.ReadDiscreteInputs, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.ReadDiscreteInputs);

        if (response.Data == null || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取离散输入响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount)
            throw new ModbusCommunicationException("读取离散输入响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data, 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToBoolArray(dataBytes, quantity);
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 125);

        var request = new ModbusRequest(slaveId, ModbusFunction.ReadHoldingRegisters, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.ReadHoldingRegisters);

        if (response.Data == null || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取保持寄存器响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || byteCount != quantity * 2)
            throw new ModbusCommunicationException("读取保持寄存器响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data, 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToUshortArray(dataBytes);
    }

    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default) {
        ValidateReadParameters(quantity, 125);

        var request = new ModbusRequest(slaveId, ModbusFunction.ReadInputRegisters, startAddress, quantity);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.ReadInputRegisters);

        if (response.Data == null || response.Data.Length < 1)
            throw new ModbusCommunicationException("读取输入寄存器响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || byteCount != quantity * 2)
            throw new ModbusCommunicationException("读取输入寄存器响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data, 1, dataBytes, 0, byteCount);

        return ModbusUtils.ByteArrayToUshortArray(dataBytes);
    }

    #endregion

    #region 泛型读取功能

    public async Task<T[]> ReadHoldingRegistersAsync<T>(byte slaveId, ushort startAddress, ushort count,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancellationToken = default) where T : unmanaged {
        if (count == 0)
            throw new ArgumentException("元素数量不能为0", nameof(count));

        var registerCount = (ushort)ModbusDataConverter.GetTotalRegisterCount<T>(count);
        ValidateReadParameters(registerCount, 125);

        var registers = await ReadHoldingRegistersAsync(slaveId, startAddress, registerCount, cancellationToken);

        // 将寄存器数据转换为字节数组
        var bytes = ModbusUtils.UshortArrayToByteArray(registers);

        // 使用泛型转换器转换为目标类型
        return ModbusDataConverter.FromBytes<T>(bytes, count, byteOrder, wordOrder);
    }

    public async Task<T[]> ReadInputRegistersAsync<T>(byte slaveId, ushort startAddress, ushort count,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancellationToken = default) where T : unmanaged {
        if (count == 0)
            throw new ArgumentException("元素数量不能为0", nameof(count));

        var registerCount = (ushort)ModbusDataConverter.GetTotalRegisterCount<T>(count);
        ValidateReadParameters(registerCount, 125);

        var registers = await ReadInputRegistersAsync(slaveId, startAddress, registerCount, cancellationToken);

        // 将寄存器数据转换为字节数组
        var bytes = ModbusUtils.UshortArrayToByteArray(registers);

        // 使用泛型转换器转换为目标类型
        return ModbusDataConverter.FromBytes<T>(bytes, count, byteOrder, wordOrder);
    }

    #endregion

    #region 写入功能

    public async Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default) {
        var data = new byte[] { (byte)(value ? 1 : 0) };
        var request = new ModbusRequest(slaveId, ModbusFunction.WriteSingleCoil, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.WriteSingleCoil);
    }

    public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default) {
        var data = new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        var request = new ModbusRequest(slaveId, ModbusFunction.WriteSingleRegister, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.WriteSingleRegister);
    }

    public async Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default) {
        if (values == null || values.Length == 0)
            throw new ArgumentException("线圈值数组不能为空", nameof(values));

        if (values.Length > 1968)
            throw new ArgumentException("线圈数量不能超过1968", nameof(values));

        var data = ModbusUtils.BoolArrayToByteArray(values);
        var request = new ModbusRequest(slaveId, ModbusFunction.WriteMultipleCoils, startAddress, (ushort)values.Length, data);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.WriteMultipleCoils);
    }

    public async Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default) {
        if (values == null || values.Length == 0)
            throw new ArgumentException("寄存器值数组不能为空", nameof(values));

        if (values.Length > 123)
            throw new ArgumentException("寄存器数量不能超过123", nameof(values));

        var data = ModbusUtils.UshortArrayToByteArray(values);
        var request = new ModbusRequest(slaveId, ModbusFunction.WriteMultipleRegisters, startAddress, (ushort)values.Length, data);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.WriteMultipleRegisters);
    }

    #endregion

    #region 泛型写入功能

    public async Task WriteSingleRegisterAsync<T>(byte slaveId, ushort address, T value,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst, CancellationToken cancellationToken = default) where T : unmanaged {
        var registerCount = ModbusDataConverter.GetRegisterCount<T>();

        if (registerCount == 1) {
            // 对于单寄存器值，直接转换
            var bytes = ModbusDataConverter.ToBytes([value], byteOrder, wordOrder);
            if (bytes.Length >= 2) {
                var registerValue = (ushort)((bytes[0] << 8) | bytes[1]);
                await WriteSingleRegisterAsync(slaveId, address, registerValue, cancellationToken);
            } else {
                throw new ArgumentException($"类型 {typeof(T).Name} 需要至少1个寄存器");
            }
        } else {
            // 对于多寄存器值，使用WriteMultipleRegisters
            await WriteMultipleRegistersAsync(slaveId, address, [value], byteOrder, wordOrder, cancellationToken);
        }
    }

    public async Task WriteMultipleRegistersAsync<T>(byte slaveId, ushort startAddress, T[] values,
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
        await WriteMultipleRegistersAsync(slaveId, startAddress, registers, cancellationToken);
    }

    #endregion

    #region 高级功能

    public async Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveId, ushort readStartAddress, ushort readQuantity,
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

        var request = new ModbusRequest(slaveId, ModbusFunction.ReadWriteMultipleRegisters, readStartAddress, readQuantity, requestData);
        var response = await ExecuteRequestAsync(request, cancellationToken);

        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.ReadWriteMultipleRegisters);

        if (response.Data == null || response.Data.Length < 1)
            throw new ModbusCommunicationException("读写多个寄存器响应数据不足");

        var byteCount = response.Data[0];
        if (response.Data.Length < 1 + byteCount || byteCount != readQuantity * 2)
            throw new ModbusCommunicationException("读写多个寄存器响应数据长度不匹配");

        var dataBytes = new byte[byteCount];
        Array.Copy(response.Data, 1, dataBytes, 0, byteCount);

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

    public bool[] ReadCoils(byte slaveId, ushort startAddress, ushort quantity) {
        return ReadCoilsAsync(slaveId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    public bool[] ReadDiscreteInputs(byte slaveId, ushort startAddress, ushort quantity) {
        return ReadDiscreteInputsAsync(slaveId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    public ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort quantity) {
        return ReadHoldingRegistersAsync(slaveId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    public ushort[] ReadInputRegisters(byte slaveId, ushort startAddress, ushort quantity) {
        return ReadInputRegistersAsync(slaveId, startAddress, quantity, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步写入功能

    public void WriteSingleCoil(byte slaveId, ushort address, bool value) {
        WriteSingleCoilAsync(slaveId, address, value, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteSingleRegister(byte slaveId, ushort address, ushort value) {
        WriteSingleRegisterAsync(slaveId, address, value, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleCoils(byte slaveId, ushort startAddress, bool[] values) {
        WriteMultipleCoilsAsync(slaveId, startAddress, values, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleRegisters(byte slaveId, ushort startAddress, ushort[] values) {
        WriteMultipleRegistersAsync(slaveId, startAddress, values, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步高级功能

    public ushort[] ReadWriteMultipleRegisters(byte slaveId, ushort readStartAddress, ushort readQuantity,
        ushort writeStartAddress, ushort[] writeValues) {
        return ReadWriteMultipleRegistersAsync(slaveId, readStartAddress, readQuantity, writeStartAddress, writeValues, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    #endregion

    #region 同步泛型读取功能

    public T[] ReadHoldingRegisters<T>(byte slaveId, ushort startAddress, ushort count,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        return ReadHoldingRegistersAsync<T>(slaveId, startAddress, count, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public T[] ReadInputRegisters<T>(byte slaveId, ushort startAddress, ushort count,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        return ReadInputRegistersAsync<T>(slaveId, startAddress, count, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region 同步泛型写入功能

    public void WriteSingleRegister<T>(byte slaveId, ushort address, T value,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        WriteSingleRegisterAsync<T>(slaveId, address, value, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void WriteMultipleRegisters<T>(byte slaveId, ushort startAddress, T[] values,
        ByteOrder byteOrder = ByteOrder.BigEndian, WordOrder wordOrder = WordOrder.HighFirst) where T : unmanaged {
        WriteMultipleRegistersAsync<T>(slaveId, startAddress, values, byteOrder, wordOrder, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    protected async Task<ModbusResponse> ExecuteRequestAsync(ModbusRequest request, CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected)
            throw new ModbusConnectionException("客户端未连接");

        Exception? lastException = null;

        for (int attempt = 0; attempt <= Retries; attempt++) {
            try {
                var requestBytes = _protocol.BuildRequest(request);
                var responseBytes = await _transport.SendReceiveAsync(requestBytes, cancellationToken);

                if (!_protocol.ValidateResponse(responseBytes))
                    throw new ModbusCommunicationException("响应数据验证失败");

                return _protocol.ParseResponse(responseBytes, request);
            } catch (Exception ex) when (attempt < Retries && IsRetryableException(ex)) {
                lastException = ex;
                await Task.Delay(100 * (attempt + 1), cancellationToken); // 递增延迟
            }
        }

        throw lastException ?? new ModbusCommunicationException("请求执行失败");
    }

    private static bool IsRetryableException(Exception exception) {
        return exception is ModbusTimeoutException ||
               exception is ModbusCommunicationException ||
               (exception is ModbusException modbusEx &&
                modbusEx.ExceptionCode == ModbusExceptionCode.SlaveDeviceBusy);
    }

    private static void ValidateReadParameters(ushort quantity, int maxQuantity) {
        if (quantity == 0)
            throw new ArgumentException("数量不能为0", nameof(quantity));

        if (quantity > maxQuantity)
            throw new ArgumentException($"数量不能超过{maxQuantity}", nameof(quantity));
    }

    public virtual void Dispose() {
        if (_disposed)
            return;

        _disposed = true;
        _transport?.Dispose();
        GC.SuppressFinalize(this);
    }

    public virtual async ValueTask DisposeAsync() {
        if (_disposed)
            return;

        _disposed = true;

        if (_transport != null) {
            try {
                await DisconnectAsync();
            } catch {
                // 忽略断开连接时的异常
            }

            // 优先使用异步释放，回退到同步释放
            if (_transport is IAsyncDisposable asyncDisposableTransport) {
                await asyncDisposableTransport.DisposeAsync();
            } else {
                _transport.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }
}