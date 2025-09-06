using ModbusLib.Enums;

namespace ModbusLib.Interfaces;

/// <summary>
/// Modbus客户端接口
/// </summary>
public interface IModbusClient : IDisposable
{
    #region 连接管理

    /// <summary>
    /// 异步连接到Modbus设备
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否成功</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步断开连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// </summary>
    bool IsConnected { get; }

    #endregion

    #region 读取功能

    /// <summary>
    /// 读取线圈状态
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="quantity">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>线圈状态数组</returns>
    Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取离散输入状态
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="quantity">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>离散输入状态数组</returns>
    Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取保持寄存器
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="quantity">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>寄存器值数组</returns>
    Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="quantity">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>寄存器值数组</returns>
    Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    #endregion

    #region 写入功能

    /// <summary>
    /// 写单个线圈
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="address">线圈地址</param>
    /// <param name="value">线圈值</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写单个寄存器
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="value">寄存器值</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写多个线圈
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">线圈值数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写多个寄存器
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">寄存器值数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);

    #endregion

    #region 高级功能

    /// <summary>
    /// 读写多个寄存器
    /// </summary>
    /// <param name="slaveId">从站地址</param>
    /// <param name="readStartAddress">读取起始地址</param>
    /// <param name="readQuantity">读取数量</param>
    /// <param name="writeStartAddress">写入起始地址</param>
    /// <param name="writeValues">写入值数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取的寄存器值数组</returns>
    Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveId, ushort readStartAddress, ushort readQuantity,
        ushort writeStartAddress, ushort[] writeValues, CancellationToken cancellationToken = default);

    #endregion

    #region 泛型读取功能
    
    /// <summary>
    /// 泛型读取保持寄存器
    /// </summary>
    /// <typeparam name="T">数据类型 (支持 unmanaged 类型)</typeparam>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">要返回的T类型元素数量 (例如: count=10且T=byte时返回10个byte值，实际需要5个寄存器)</param>
    /// <param name="endianness">字节序模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定类型的数组，长度为count</returns>
    Task<T[]> ReadHoldingRegistersAsync<T>(byte slaveId, ushort startAddress, ushort count, 
        ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) 
        where T : unmanaged;
    
    /// <summary>
    /// 泛型读取输入寄存器
    /// </summary>
    /// <typeparam name="T">数据类型 (支持 unmanaged 类型)</typeparam>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">要返回的T类型元素数量 (例如: count=10且T=byte时返回10个byte值，实际需要5个寄存器)</param>
    /// <param name="endianness">字节序模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定类型的数组，长度为count</returns>
    Task<T[]> ReadInputRegistersAsync<T>(byte slaveId, ushort startAddress, ushort count,
        ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) 
        where T : unmanaged;
    
    #endregion

    #region 泛型写入功能
    
    /// <summary>
    /// 泛型写入单个寄存器
    /// </summary>
    /// <typeparam name="T">数据类型 (支持 unmanaged 类型)</typeparam>
    /// <param name="slaveId">从站地址</param>
    /// <param name="address">寄存器地址</param>
    /// <param name="value">要写入的值</param>
    /// <param name="endianness">字节序模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteSingleRegisterAsync<T>(byte slaveId, ushort address, T value,
        ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) 
        where T : unmanaged;
    
    /// <summary>
    /// 泛型写入多个寄存器
    /// </summary>
    /// <typeparam name="T">数据类型 (支持 unmanaged 类型)</typeparam>
    /// <param name="slaveId">从站地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">要写入的值数组</param>
    /// <param name="endianness">字节序模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteMultipleRegistersAsync<T>(byte slaveId, ushort startAddress, T[] values,
        ModbusEndianness endianness = ModbusEndianness.BigEndian, CancellationToken cancellationToken = default) 
        where T : unmanaged;
    
    #endregion

    #region 配置属性

    /// <summary>
    /// 超时时间
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    int Retries { get; set; }

    #endregion
}