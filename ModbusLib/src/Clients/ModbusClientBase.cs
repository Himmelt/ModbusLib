using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Protocols;

namespace ModbusLib.Clients;

/// <summary>
/// Modbus客户端基类
/// </summary>
public abstract class ModbusClientBase : IModbusClient
{
    protected readonly IModbusTransport _transport;
    protected readonly IModbusProtocol _protocol;
    protected bool _disposed = false;

    public TimeSpan Timeout 
    { 
        get => _transport.Timeout;
        set => _transport.Timeout = value;
    }
    
    public int Retries { get; set; } = 3;

    public bool IsConnected => _transport.IsConnected;

    protected ModbusClientBase(IModbusTransport transport, IModbusProtocol protocol)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
    }

    public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);

        return await _transport.ConnectAsync(cancellationToken);
    }

    public virtual async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        await _transport.DisconnectAsync(cancellationToken);
    }

    #region 读取功能

    public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
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

    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
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

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
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

    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
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

    #region 写入功能

    public async Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default)
    {
        var data = new byte[] { (byte)(value ? 1 : 0) };
        var request = new ModbusRequest(slaveId, ModbusFunction.WriteSingleCoil, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancellationToken);
        
        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.WriteSingleCoil);
    }

    public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        var data = new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        var request = new ModbusRequest(slaveId, ModbusFunction.WriteSingleRegister, address, 1, data);
        var response = await ExecuteRequestAsync(request, cancellationToken);
        
        if (response.IsError)
            throw new ModbusException(response.ExceptionCode!.Value, slaveId, ModbusFunction.WriteSingleRegister);
    }

    public async Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default)
    {
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

    public async Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
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

    #region 高级功能

    public async Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveId, ushort readStartAddress, ushort readQuantity,
        ushort writeStartAddress, ushort[] writeValues, CancellationToken cancellationToken = default)
    {
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

    protected async Task<ModbusResponse> ExecuteRequestAsync(ModbusRequest request, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);

        if (!IsConnected)
            throw new ModbusConnectionException("客户端未连接");

        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= Retries; attempt++)
        {
            try
            {
                var requestBytes = _protocol.BuildRequest(request);
                var responseBytes = await _transport.SendReceiveAsync(requestBytes, cancellationToken);
                
                if (!_protocol.ValidateResponse(responseBytes))
                    throw new ModbusCommunicationException("响应数据验证失败");
                
                return _protocol.ParseResponse(responseBytes, request);
            }
            catch (Exception ex) when (attempt < Retries && IsRetryableException(ex))
            {
                lastException = ex;
                await Task.Delay(100 * (attempt + 1), cancellationToken); // 递增延迟
            }
        }
        
        throw lastException ?? new ModbusCommunicationException("请求执行失败");
    }

    private static bool IsRetryableException(Exception exception)
    {
        return exception is ModbusTimeoutException ||
               exception is ModbusCommunicationException ||
               (exception is ModbusException modbusEx && 
                modbusEx.ExceptionCode == ModbusExceptionCode.SlaveDeviceBusy);
    }

    private static void ValidateReadParameters(ushort quantity, int maxQuantity)
    {
        if (quantity == 0)
            throw new ArgumentException("数量不能为0", nameof(quantity));
            
        if (quantity > maxQuantity)
            throw new ArgumentException($"数量不能超过{maxQuantity}", nameof(quantity));
    }

    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _transport?.Dispose();
    }
}