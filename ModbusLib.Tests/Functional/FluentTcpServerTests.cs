using FluentModbus;
using ModbusLib.Factories;
using ModbusLib.Models;
using ModbusLib.Transports;
using System.Net;
using Xunit.Abstractions;
using ModbusLibClient = ModbusLib.Interfaces.IModbusClient;

namespace ModbusLib.Tests.Functional;

public class FluentTcpServerTests(ITestOutputHelper output) : IDisposable {

    // 使用504端口避免与系统其他服务冲突
    private const int ServerPort = 504;
    private const byte UnitId = 3;
    private ModbusTcpServer? _mbus_erver;
    private bool _disposed;

    [Fact]
    public async Task ModbusTcp_Coils_Test() {
        try {
            // 启动FluentModbus服务器
            StartFluentModbusServer();
            output.WriteLine($"FluentModbus服务器已启动，端口: {ServerPort}");

            // 使用我们自己的客户端连接到从机
            var config = new NetworkConnectionConfig {
                Host = "127.0.0.1",
                Port = ServerPort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };

            ModbusLibClient client = ModbusClientFactory.CreateTcpClient(config);
            output.WriteLine("ModbusLib客户端已创建");

            var isConnected = await client.ConnectAsync();
            output.WriteLine($"客户端连接结果: {isConnected}");

            Assert.True(isConnected, "客户端连接失败");

            #region 测试单线圈读写

            // 测试写入单个线圈
            output.WriteLine("开始写入单个线圈，地址0");
            await client.WriteSingleCoilAsync(UnitId, 0, true);
            output.WriteLine("单个线圈写入完成");

            // 测试读取单个线圈
            output.WriteLine("开始读取单个线圈，地址0");
            var singleCoil = await client.ReadCoilsAsync(UnitId, 0, 1);
            output.WriteLine($"单个线圈读取完成，值: {singleCoil[0]}");

            // 测试写入单个线圈
            output.WriteLine("开始写入单个线圈，地址345");
            await client.WriteSingleCoilAsync(UnitId, 345, true);
            output.WriteLine("单个线圈写入完成");

            // 测试读取单个线圈
            output.WriteLine("开始读取单个线圈，地址345");
            var singleCoil2 = await client.ReadCoilsAsync(UnitId, 345, 1);
            output.WriteLine($"单个线圈读取完成，值: {singleCoil[0]}");

            // 验证结果
            Assert.True(singleCoil[0]);
            Assert.True(singleCoil2[0]);

            #endregion

            #region 测试多线圈读写

            // 测试写入多个线圈
            var coilValuesToWrite = new bool[] { true, false, true, true, false };
            output.WriteLine("开始写入多个线圈，地址10");
            await client.WriteMultipleCoilsAsync(UnitId, 10, coilValuesToWrite);
            output.WriteLine("多个线圈写入完成");

            // 测试读取多个线圈
            output.WriteLine("开始读取多个线圈，地址10");
            var multipleCoils = await client.ReadCoilsAsync(UnitId, 10, 5);
            output.WriteLine($"多个线圈读取完成，值: {string.Join(", ", multipleCoils)}");

            var coilValuesToWrite2 = new bool[] { true, false, true, false, true, false, true };
            output.WriteLine("开始写入多个线圈，地址456");
            await client.WriteMultipleCoilsAsync(UnitId, 456, coilValuesToWrite);
            output.WriteLine("多个线圈写入完成");

            // 测试读取多个线圈
            output.WriteLine("开始读取多个线圈，地址456");
            var multipleCoils2 = await client.ReadCoilsAsync(UnitId, 456, 7);
            output.WriteLine($"多个线圈读取完成，值: {string.Join(", ", multipleCoils)}");

            // 验证结果
            Assert.Equal(coilValuesToWrite, multipleCoils);
            Assert.Equal(coilValuesToWrite2, multipleCoils2);

            #endregion
        } finally {
            // 清理资源
        }
    }

    [Fact]
    public async Task ModbusTcp_Registers_Test() {
        try {
            // 启动FluentModbus从机
            StartFluentModbusServer();
            output.WriteLine($"FluentModbus从机已启动，端口: {SlavePort}");

            // 使用我们自己的客户端连接到从机
            var config = new NetworkConnectionConfig {
                Host = "127.0.0.1",
                Port = SlavePort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };

            ModbusLibClient client = ModbusClientFactory.CreateTcpClient(config);
            output.WriteLine("ModbusLib客户端已创建");

            var isConnected = await client.ConnectAsync();
            output.WriteLine($"客户端连接结果: {isConnected}");

            // 如果是TcpTransport，获取详细连接状态
            if (client is ModbusLib.Clients.ModbusTcpClient tcpClient) {
                // 这里我们需要通过反射获取transport对象
                var transportField = typeof(ModbusLib.Clients.ModbusClientBase).GetField("_transport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (transportField != null) {
                    var transport = transportField.GetValue(tcpClient);
                    if (transport is TcpTransport tcpTransport) {
                    }
                }
            }

            Assert.True(isConnected, "客户端连接失败");

            // 测试写入单个寄存器
            output.WriteLine("开始写入单个寄存器");
            await client.WriteSingleRegisterAsync(UnitId, 0, 12345);
            output.WriteLine("单个寄存器写入完成");

            // 测试写入多个寄存器
            ushort[] writeValues = [100, 200, 300, 400, 500];
            output.WriteLine("开始写入多个寄存器");
            // 明确指定调用非泛型版本
            await client.WriteMultipleRegistersAsync(UnitId, 10, writeValues, default(CancellationToken));
            output.WriteLine("多个寄存器写入完成");

            // 测试读取单个寄存器
            output.WriteLine("开始读取单个寄存器");
            var singleRegister = await client.ReadHoldingRegistersAsync(UnitId, 0, 1);
            output.WriteLine($"单个寄存器读取完成，值: {singleRegister[0]}");

            // 测试读取多个寄存器
            output.WriteLine("开始读取多个寄存器");
            var multipleRegisters = await client.ReadHoldingRegistersAsync(UnitId, 10, 5);
            output.WriteLine($"多个寄存器读取完成，值: {string.Join(", ", multipleRegisters)}");

            // 验证结果
            Assert.Equal(12345, singleRegister[0]);
            Assert.Equal(writeValues, multipleRegisters);

            output.WriteLine($"单个寄存器值: {singleRegister[0]}");
            output.WriteLine($"多个寄存器值: {string.Join(", ", multipleRegisters)}");
        } finally {
            // 清理资源
        }
    }

    [Fact]
    public async Task ModbusTcp_GenericRegisters_Test() {
        try {
            // 启动FluentModbus从机
            StartFluentModbusServer();
            output.WriteLine($"FluentModbus从机已启动，端口: {SlavePort}");

            // 使用我们自己的客户端连接到从机
            var config = new NetworkConnectionConfig {
                Host = "127.0.0.1",
                Port = SlavePort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };

            ModbusLibClient client = ModbusClientFactory.CreateTcpClient(config);
            output.WriteLine("ModbusLib客户端已创建");

            var isConnected = await client.ConnectAsync();
            output.WriteLine($"客户端连接结果: {isConnected}");

            // 如果是TcpTransport，获取详细连接状态
            if (client is ModbusLib.Clients.ModbusTcpClient tcpClient) {
                // 这里我们需要通过反射获取transport对象
                var transportField = typeof(ModbusLib.Clients.ModbusClientBase).GetField("_transport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (transportField != null) {
                    var transport = transportField.GetValue(tcpClient);
                    if (transport is TcpTransport tcpTransport) {
                    }
                }
            }

            Assert.True(isConnected, "客户端连接失败");

            // 测试写入和读取浮点数数组
            output.WriteLine("开始写入浮点数数组");
            float[] floatValues = [1.23f, 4.56f, 7.89f];
            await client.WriteMultipleRegistersAsync<float>(UnitId, 0, floatValues);
            output.WriteLine("浮点数数组写入完成");

            output.WriteLine("开始读取浮点数数组");
            var readFloatValues = await client.ReadHoldingRegistersAsync<float>(UnitId, 0, 3);
            output.WriteLine($"浮点数数组读取完成，值: {string.Join(", ", readFloatValues)}");

            // 测试写入和读取双精度浮点数
            output.WriteLine("开始写入双精度浮点数");
            double doubleValue = 123.456;
            await client.WriteSingleRegisterAsync<double>(UnitId, 10, doubleValue);
            output.WriteLine("双精度浮点数写入完成");

            output.WriteLine("开始读取双精度浮点数");
            var readDoubleValue = await client.ReadHoldingRegistersAsync<double>(UnitId, 10, 1);
            output.WriteLine($"双精度浮点数读取完成，值: {readDoubleValue[0]}");

            // 验证结果
            Assert.Equal(floatValues, readFloatValues);
            Assert.Equal(doubleValue, readDoubleValue[0]);

            output.WriteLine($"浮点数组值: {string.Join(", ", readFloatValues)}");
            output.WriteLine($"双精度浮点值: {readDoubleValue[0]}");
        } finally {
            // 清理资源
        }
    }

    [Fact]
    public async Task ModbusTcp_RWMultiRegisters_Test() {
        try {
            // 启动FluentModbus从机
            StartFluentModbusServer();
            output.WriteLine($"FluentModbus从机已启动，端口: {SlavePort}");

            // 使用我们自己的客户端连接到从机
            var config = new NetworkConnectionConfig {
                Host = "127.0.0.1",
                Port = SlavePort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };

            ModbusLibClient client = ModbusClientFactory.CreateTcpClient(config);
            output.WriteLine("ModbusLib客户端已创建");

            var isConnected = await client.ConnectAsync();
            output.WriteLine($"客户端连接结果: {isConnected}");

            // 如果是TcpTransport，获取详细连接状态
            if (client is ModbusLib.Clients.ModbusTcpClient tcpClient) {
                // 这里我们需要通过反射获取transport对象
                var transportField = typeof(ModbusLib.Clients.ModbusClientBase).GetField("_transport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (transportField != null) {
                    var transport = transportField.GetValue(tcpClient);
                    if (transport is TcpTransport tcpTransport) {
                    }
                }
            }

            Assert.True(isConnected, "客户端连接失败");

            // 先写入一些数据用于读取
            output.WriteLine("开始写入初始数据");
            ushort[] initialValues = [1000, 2000, 3000];
            // 明确指定调用非泛型版本
            await client.WriteMultipleRegistersAsync(UnitId, 0, initialValues, default(CancellationToken));
            output.WriteLine("初始数据写入完成");

            // 准备要写入的数据
            ushort[] writeValues = [5000, 6000, 7000];

            // 执行读写多个寄存器操作
            output.WriteLine("开始执行读写多个寄存器操作");
            var readValues = await client.ReadWriteMultipleRegistersAsync(UnitId, 0, 3, 10, writeValues);
            output.WriteLine($"读写多个寄存器操作完成，读取值: {string.Join(", ", readValues)}");

            // 验证读取的数据是否正确
            Assert.Equal(initialValues, readValues);

            // 验证写入的数据是否正确
            output.WriteLine("开始验证写入的数据");
            var verifyValues = await client.ReadHoldingRegistersAsync(UnitId, 10, 3);
            output.WriteLine($"验证数据读取完成，值: {string.Join(", ", verifyValues)}");
            Assert.Equal(writeValues, verifyValues);

            output.WriteLine($"读取的值: {string.Join(", ", readValues)}");
            output.WriteLine($"写入的值: {string.Join(", ", verifyValues)}");
        } finally {
            // 清理资源
        }
    }

    private void StartFluentModbusServer() {
        if (_mbus_erver != null) {
            _mbus_erver.Stop();
            _mbus_erver.Dispose();
        }

        _mbus_erver = new ModbusTcpServer();
        _mbus_erver.Start(new IPEndPoint(IPAddress.Loopback, ServerPort));
        _mbus_erver.AddUnit(UnitId);

        // 等待服务器启动
        Thread.Sleep(200);
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                try {
                    _mbus_erver?.Stop();
                    _mbus_erver?.Dispose();
                } catch (Exception ex) {
                    output?.WriteLine($"释放服务器时出错: {ex.Message}");
                }
            }

            _disposed = true;
        }
    }
}
