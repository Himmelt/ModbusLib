using ModbusLib.Interfaces;
using ModbusLib.Enums;
using ModbusLib.Models;
using System.Collections.Generic;

namespace ModbusLib.Tests
{
    /// <summary>
    /// 测试用的Modbus客户端，模拟真实的客户端行为
    /// </summary>
    public class TestModbusClient : IModbusClient
    {
        private readonly Dictionary<string, ushort[]> _holdingRegisters = new();
        private readonly Dictionary<string, ushort[]> _inputRegisters = new();
        private bool _isConnected = false;
        private bool _disposed = false;
        
        public bool IsConnected => _isConnected;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
        public int Retries { get; set; } = 3;
        
        // 测试跟踪属性
        public bool WriteSingleRegisterCalled { get; private set; }
        public bool WriteMultipleRegistersCalled { get; private set; }
        
        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            _isConnected = true;
            return Task.FromResult(true);
        }
        
        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _isConnected = false;
            return Task.CompletedTask;
        }
        
        public Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
        {
            var key = $"{slaveId}:{startAddress}:{quantity}";
            if (_holdingRegisters.TryGetValue(key, out var registers))
            {
                return Task.FromResult(registers);
            }
            
            // 默认返回递增数值
            var result = new ushort[quantity];
            for (int i = 0; i < quantity; i++)
            {
                result[i] = (ushort)(startAddress + i);
            }
            return Task.FromResult(result);
        }
        
        public Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
        {
            var key = $"{slaveId}:{startAddress}:{quantity}";
            if (_inputRegisters.TryGetValue(key, out var registers))
            {
                return Task.FromResult(registers);
            }
            
            // 默认返回递增数值
            var result = new ushort[quantity];
            for (int i = 0; i < quantity; i++)
            {
                result[i] = (ushort)(startAddress + i);
            }
            return Task.FromResult(result);
        }
        
        public Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default)
        {
            WriteSingleRegisterCalled = true;
            return Task.CompletedTask;
        }
        
        public Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        public Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
        {
            WriteMultipleRegistersCalled = true;
            return Task.CompletedTask;
        }
        
        public Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveId, ushort readStartAddress, ushort readQuantity, ushort writeStartAddress, ushort[] writeValues, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        // 泛型读取方法
        public async Task<T[]> ReadHoldingRegistersAsync<T>(byte slaveId, ushort startAddress, ushort count, 
            ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) where T : unmanaged
        {
            if (count == 0)
                throw new ArgumentException("元素数量不能为0", nameof(count));
            
            var registerCount = (ushort)ModbusDataConverter.GetTotalRegisterCount<T>(count);
            var registers = await ReadHoldingRegistersAsync(slaveId, startAddress, registerCount, cancellationToken);
            
            // 将寄存器数据转换为字节数组
            var bytes = new List<byte>();
            foreach (var register in registers)
            {
                bytes.Add((byte)(register >> 8));
                bytes.Add((byte)(register & 0xFF));
            }
            
            // 使用泛型转换器转换为目标类型
            return ModbusDataConverter.FromBytes<T>(bytes.ToArray(), count, endianness);
        }
        
        public async Task<T[]> ReadInputRegistersAsync<T>(byte slaveId, ushort startAddress, ushort count,
            ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) where T : unmanaged
        {
            if (count == 0)
                throw new ArgumentException("元素数量不能为0", nameof(count));
            
            var registerCount = (ushort)ModbusDataConverter.GetTotalRegisterCount<T>(count);
            var registers = await ReadInputRegistersAsync(slaveId, startAddress, registerCount, cancellationToken);
            
            // 将寄存器数据转换为字节数组
            var bytes = new List<byte>();
            foreach (var register in registers)
            {
                bytes.Add((byte)(register >> 8));
                bytes.Add((byte)(register & 0xFF));
            }
            
            // 使用泛型转换器转换为目标类型
            return ModbusDataConverter.FromBytes<T>(bytes.ToArray(), count, endianness);
        }
        
        // 泛型写入方法
        public async Task WriteSingleRegisterAsync<T>(byte slaveId, ushort address, T value,
            ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) where T : unmanaged
        {
            var registerCount = ModbusDataConverter.GetRegisterCount<T>();
            
            if (registerCount == 1)
            {
                // 对于单寄存器值，直接转换
                var bytes = ModbusDataConverter.ToBytes(new[] { value }, endianness);
                if (bytes.Length >= 2)
                {
                    var registerValue = (ushort)((bytes[0] << 8) | bytes[1]);
                    await WriteSingleRegisterAsync(slaveId, address, registerValue, cancellationToken);
                }
                else
                {
                    throw new ArgumentException($"类型 {typeof(T).Name} 需要至少1个寄存器");
                }
            }
            else
            {
                // 对于多寄存器值，使用WriteMultipleRegisters
                await WriteMultipleRegistersAsync(slaveId, address, new[] { value }, endianness, cancellationToken);
            }
        }
        
        public async Task WriteMultipleRegistersAsync<T>(byte slaveId, ushort startAddress, T[] values,
            ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) where T : unmanaged
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("值数组不能为空", nameof(values));
            
            var registerCount = ModbusDataConverter.GetTotalRegisterCount<T>(values.Length);
            if (registerCount > 123)
                throw new ArgumentException($"所需寄存器数量({registerCount})不能超过123", nameof(values));
            
            // 将泛型数组转换为字节数组
            var bytes = ModbusDataConverter.ToBytes(values, endianness);
            
            // 将字节数组转换为寄存器数组
            var registers = TestHelper.BytesToUshortArray(bytes);
            
            // 调用原始写入方法
            await WriteMultipleRegistersAsync(slaveId, startAddress, registers, cancellationToken);
        }
        
        // 测试辅助方法
        public void SetupReadHoldingRegistersResponse(byte slaveId, ushort startAddress, ushort quantity, ushort[] registers)
        {
            var key = $"{slaveId}:{startAddress}:{quantity}";
            _holdingRegisters[key] = registers;
        }
        
        public void SetupReadInputRegistersResponse(byte slaveId, ushort startAddress, ushort quantity, ushort[] registers)
        {
            var key = $"{slaveId}:{startAddress}:{quantity}";
            _inputRegisters[key] = registers;
        }
        
        public void SetupWriteSingleRegisterResponse(byte slaveId, ushort address)
        {
            // 写入操作的模拟设置
        }
        
        public void SetupWriteMultipleRegistersResponse(byte slaveId, ushort startAddress)
        {
            // 写入操作的模拟设置
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _isConnected = false;
        }
    }
}