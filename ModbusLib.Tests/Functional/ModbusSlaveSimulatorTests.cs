using System.Net;
using System.Net.Sockets;
using NModbus;
using NModbus.Device;
using Xunit;
using Xunit.Abstractions;

namespace ModbusLib.Tests.Functional;

/// <summary>
/// 使用NModbus模拟从机的Modbus功能测试
/// </summary>
public class ModbusSlaveSimulatorTests
{
    private readonly ITestOutputHelper _output;
    private const int SlavePort = 503; // 使用503端口避免与系统其他服务冲突
    private const byte SlaveId = 1;

    public ModbusSlaveSimulatorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试使用NModbus创建TCP从机并进行读写操作
    /// </summary>
    [Fact]
    public async Task ModbusTcpSlave_ReadWriteCoils_Test()
    {
        // 启动从机模拟器
        using var slaveSimulator = new ModbusSlaveSimulator(SlavePort, SlaveId);
        await slaveSimulator.StartAsync();

        // 创建主站客户端连接到从机
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(IPAddress.Loopback, SlavePort);
        
        var factory = new ModbusFactory();
        var master = factory.CreateMaster(tcpClient);

        // 测试写入单个线圈
        await master.WriteSingleCoilAsync(SlaveId, 0, true);
        
        // 测试读取线圈
        var coilValues = await master.ReadCoilsAsync(SlaveId, 0, 10);
        
        // 验证结果
        Assert.NotNull(coilValues);
        Assert.True(coilValues[0]); // 第一个线圈应该为true
        Assert.False(coilValues[1]); // 其他线圈应该为false（默认值）
        
        _output.WriteLine($"读取线圈值: {string.Join(", ", coilValues)}");
    }

    /// <summary>
    /// 测试使用NModbus创建TCP从机并进行寄存器读写操作
    /// </summary>
    [Fact]
    public async Task ModbusTcpSlave_ReadWriteRegisters_Test()
    {
        // 启动从机模拟器
        using var slaveSimulator = new ModbusSlaveSimulator(SlavePort, SlaveId);
        await slaveSimulator.StartAsync();

        // 创建主站客户端连接到从机
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(IPAddress.Loopback, SlavePort);
        
        var factory = new ModbusFactory();
        var master = factory.CreateMaster(tcpClient);

        // 测试写入单个寄存器
        await master.WriteSingleRegisterAsync(SlaveId, 0, 12345);
        
        // 测试写入多个寄存器
        ushort[] writeValues = [100, 200, 300];
        await master.WriteMultipleRegistersAsync(SlaveId, 10, writeValues);
        
        // 测试读取单个寄存器
        var singleRegister = await master.ReadHoldingRegistersAsync(SlaveId, 0, 1);
        
        // 测试读取多个寄存器
        var multipleRegisters = await master.ReadHoldingRegistersAsync(SlaveId, 10, 3);
        
        // 验证结果
        Assert.Equal(12345, singleRegister[0]);
        Assert.Equal(writeValues, multipleRegisters);
        
        _output.WriteLine($"单个寄存器值: {singleRegister[0]}");
        _output.WriteLine($"多个寄存器值: {string.Join(", ", multipleRegisters)}");
    }

    /// <summary>
    /// 测试使用我们自己的Modbus客户端连接到NModbus从机
    /// </summary>
    [Fact]
    public async Task ModbusLibClient_ConnectToNModbusSlave_Test()
    {
        // 启动从机模拟器
        using var slaveSimulator = new ModbusSlaveSimulator(SlavePort, SlaveId);
        await slaveSimulator.StartAsync();

        // 使用我们自己的客户端连接到从机
        var config = new NetworkConnectionConfig
        {
            Host = "127.0.0.1",
            Port = SlavePort
        };

        var client = ModbusClientFactory.CreateTcpClient(config);
        
        try
        {
            await client.ConnectAsync();
            
            // 测试写入和读取线圈
            await client.WriteSingleCoilAsync(SlaveId, 0, true);
            var coilValue = await client.ReadCoilsAsync(SlaveId, 0, 1);
            
            // 测试写入和读取寄存器
            await client.WriteSingleRegisterAsync(SlaveId, 0, 54321);
            var registerValue = await client.ReadHoldingRegistersAsync(SlaveId, 0, 1);
            
            // 验证结果
            Assert.True(coilValue[0]);
            Assert.Equal(54321, registerValue[0]);
            
            _output.WriteLine($"线圈值: {coilValue[0]}");
            _output.WriteLine($"寄存器值: {registerValue[0]}");
        }
        finally
        {
            await client.DisconnectAsync();
        }
    }
}

/// <summary>
/// Modbus从机模拟器，使用NModbus库实现
/// </summary>
public class ModbusSlaveSimulator : IDisposable
{
    private readonly int _port;
    private readonly byte _slaveId;
    private TcpListener? _tcpListener;
    private IModbusSlaveNetwork? _slaveNetwork;
    private Task? _listenTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public ModbusSlaveSimulator(int port, byte slaveId)
    {
        _port = port;
        _slaveId = slaveId;
    }

    public async Task StartAsync()
    {
        _tcpListener = new TcpListener(IPAddress.Any, _port);
        _tcpListener.Start();

        var factory = new ModbusFactory();
        _slaveNetwork = factory.CreateSlaveNetwork(_tcpListener);
        
        // 创建从机数据存储
        var dataStore = new SlaveStorage();
        var slave = factory.CreateSlave(_slaveId, dataStore);
        _slaveNetwork.AddSlave(slave);

        // 启动监听任务
        _cancellationTokenSource = new CancellationTokenSource();
        _listenTask = Task.Run(async () =>
        {
            try
            {
                await _slaveNetwork.ListenAsync(_cancellationTokenSource.Token);
            }
            catch (ObjectDisposedException)
            {
                // 正常关闭时会抛出此异常，可以忽略
            }
        });
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        
        if (_tcpListener != null)
        {
            _tcpListener.Stop();
            _tcpListener = null;
        }
        
        if (_listenTask != null && !_listenTask.IsCompleted)
        {
            try
            {
                _listenTask.Wait(1000); // 等待最多1秒
            }
            catch (AggregateException)
            {
                // 忽略等待异常
            }
        }
        
        _cancellationTokenSource?.Dispose();
        _slaveNetwork?.Dispose();
    }
}

/// <summary>
/// 从机数据存储实现
/// </summary>
public class SlaveStorage : ISlaveDataStore
{
    private readonly HoldingRegisters _holdingRegisters = new();
    private readonly InputRegisters _inputRegisters = new();
    private readonly Coils _coils = new();
    private readonly DiscreteInputs _discreteInputs = new();

    public IPointSource<ushort> HoldingRegisters => _holdingRegisters;
    public IPointSource<ushort> InputRegisters => _inputRegisters;
    public IPointSource<bool> Coils => _coils;
    public IPointSource<bool> DiscreteInputs => _discreteInputs;
}

public class HoldingRegisters : IPointSource<ushort>
{
    private readonly ushort[] _registers = new ushort[65536];

    public ushort this[ushort address]
    {
        get => _registers[address];
        set => _registers[address] = value;
    }
    
    public ushort[] ReadPoints(ushort startAddress, ushort numberOfPoints)
    {
        var result = new ushort[numberOfPoints];
        Array.Copy(_registers, startAddress, result, 0, numberOfPoints);
        return result;
    }
    
    public void WritePoints(ushort startAddress, ushort[] points)
    {
        Array.Copy(points, 0, _registers, startAddress, points.Length);
    }
}

public class InputRegisters : IPointSource<ushort>
{
    private readonly ushort[] _registers = new ushort[65536];

    public ushort this[ushort address]
    {
        get => _registers[address];
        set => _registers[address] = value;
    }
    
    public ushort[] ReadPoints(ushort startAddress, ushort numberOfPoints)
    {
        var result = new ushort[numberOfPoints];
        Array.Copy(_registers, startAddress, result, 0, numberOfPoints);
        return result;
    }
    
    public void WritePoints(ushort startAddress, ushort[] points)
    {
        Array.Copy(points, 0, _registers, startAddress, points.Length);
    }
}

public class Coils : IPointSource<bool>
{
    private readonly bool[] _coils = new bool[65536];

    public bool this[ushort address]
    {
        get => _coils[address];
        set => _coils[address] = value;
    }
    
    public bool[] ReadPoints(ushort startAddress, ushort numberOfPoints)
    {
        var result = new bool[numberOfPoints];
        Array.Copy(_coils, startAddress, result, 0, numberOfPoints);
        return result;
    }
    
    public void WritePoints(ushort startAddress, bool[] points)
    {
        Array.Copy(points, 0, _coils, startAddress, points.Length);
    }
}

public class DiscreteInputs : IPointSource<bool>
{
    private readonly bool[] _inputs = new bool[65536];

    public bool this[ushort address]
    {
        get => _inputs[address];
        set => _inputs[address] = value;
    }
    
    public bool[] ReadPoints(ushort startAddress, ushort numberOfPoints)
    {
        var result = new bool[numberOfPoints];
        Array.Copy(_inputs, startAddress, result, 0, numberOfPoints);
        return result;
    }
    
    public void WritePoints(ushort startAddress, bool[] points)
    {
        Array.Copy(points, 0, _inputs, startAddress, points.Length);
    }
}