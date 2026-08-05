using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Models;
using ModbusLib.Tests.FluentServer;
using System.Net;
using ModbusLibClient = ModbusLib.Interfaces.IModbusClient;

namespace ModbusLib.Tests.Functional;

public class FluentRtuOverTcpServerTests(ITestOutputHelper output) : IDisposable {

    // 每个目标框架使用不同端口，避免多框架并行测试时端口冲突
#if NET10_0
    private const int ServerPort = 509;
#elif NET9_0
    private const int ServerPort = 507;
#else
    private const int ServerPort = 505;
#endif
    private const byte UnitId = 3;
    private ModbusRtuOverTcpServer? _mbus_erver;
    private bool _disposed;

    [Fact]
    public async Task ModbusRtuOverTcp_Coils_Test() {
        try {
            // 启动FluentModbus服务器
            StartFluentModbusServer();

            // 使用我们自己的RTU over TCP客户端连接到服务器
            var config = new NetworkConfig {
                RemoteHost = "127.0.0.1",
                RemotePort = ServerPort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };

            ModbusLibClient client = new ModbusTcpClient(config, ProtocolType.Rtu);
            output.WriteLine("ModbusLib RTU over TCP客户端已创建");

            var isConnected = await client.ConnectAsync(TestContext.Current.CancellationToken);
            output.WriteLine($"客户端连接结果: {isConnected}");

            Assert.True(isConnected, "客户端连接失败");

            #region 测试单线圈读写

            // 测试写入单个线圈
            output.WriteLine("开始写入单个线圈，地址0");
            await client.WriteSingleCoilAsync(UnitId, 0, true, TestContext.Current.CancellationToken);
            output.WriteLine("单个线圈写入完成");

            // 测试读取单个线圈
            output.WriteLine("开始读取单个线圈，地址0");
            var singleCoil = await client.ReadCoilsAsync(UnitId, 0, 1, TestContext.Current.CancellationToken);
            output.WriteLine($"单个线圈读取完成，值: {singleCoil[0]}");

            // 测试写入单个线圈
            output.WriteLine("开始写入单个线圈，地址345");
            await client.WriteSingleCoilAsync(UnitId, 345, true, TestContext.Current.CancellationToken);
            output.WriteLine("单个线圈写入完成");

            // 测试读取单个线圈
            output.WriteLine("开始读取单个线圈，地址345");
            var singleCoil2 = await client.ReadCoilsAsync(UnitId, 345, 1, TestContext.Current.CancellationToken);
            output.WriteLine($"单个线圈读取完成，值: {singleCoil[0]}");

            // 验证结果
            Assert.True(singleCoil[0]);
            Assert.True(singleCoil2[0]);

            #endregion

            #region 测试多线圈读写

            // 测试写入多个线圈
            var coilValuesToWrite = new bool[] { true, false, true, true, false };
            output.WriteLine("开始写入多个线圈，地址10");
            await client.WriteMultipleCoilsAsync(UnitId, 10, coilValuesToWrite, TestContext.Current.CancellationToken);
            output.WriteLine("多个线圈写入完成");

            // 测试读取多个线圈
            output.WriteLine("开始读取多个线圈，地址10");
            var multipleCoils = await client.ReadCoilsAsync(UnitId, 10, 5, TestContext.Current.CancellationToken);
            output.WriteLine($"多个线圈读取完成，值: {string.Join(", ", multipleCoils)}");

            var coilValuesToWrite2 = new bool[] { true, false, true, false, true, false, true };
            output.WriteLine("开始写入多个线圈，地址456");
            await client.WriteMultipleCoilsAsync(UnitId, 456, coilValuesToWrite2, TestContext.Current.CancellationToken);
            output.WriteLine("多个线圈写入完成");

            // 测试读取多个线圈
            output.WriteLine("开始读取多个线圈，地址456");
            var multipleCoils2 = await client.ReadCoilsAsync(UnitId, 456, 7, TestContext.Current.CancellationToken);
            output.WriteLine($"多个线圈读取完成，值: {string.Join(", ", multipleCoils2)}");

            // 验证结果
            Assert.Equal(coilValuesToWrite, multipleCoils);
            Assert.Equal(coilValuesToWrite2, multipleCoils2);

            #endregion
        } finally {
            // 清理资源
        }
    }

    [Fact]
    public async Task ModbusRtuOverTcp_Registers_Test() {
        try {
            // 启动FluentModbus服务器
            StartFluentModbusServer();
            output.WriteLine($"FluentModbus服务器已启动，端口: {ServerPort}");

            // 使用我们自己的RTU over TCP客户端连接到服务器
            var config = new NetworkConfig {
                RemoteHost = "127.0.0.1",
                RemotePort = ServerPort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };

            ModbusLibClient client = new ModbusTcpClient(config, ProtocolType.Rtu);
            output.WriteLine("ModbusLib RTU over TCP客户端已创建");

            var isConnected = await client.ConnectAsync(TestContext.Current.CancellationToken);
            output.WriteLine($"客户端连接结果: {isConnected}");

            Assert.True(isConnected, "客户端连接失败");

            #region 测试单寄存器读写

            // 测试写入单个寄存器
            output.WriteLine("开始写入单个寄存器，地址0");
            await client.WriteSingleRegisterAsync(UnitId, 0, 12345, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine("单个寄存器写入完成");

            // 测试读取单个寄存器
            output.WriteLine("开始读取单个寄存器，地址0");
            var singleRegister = await client.ReadHoldingRegistersAsync(UnitId, 0, 1, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"单个寄存器读取完成，值: {singleRegister[0]}");

            // 测试写入单个寄存器
            output.WriteLine("开始写入单个寄存器，地址345");
            await client.WriteSingleRegisterAsync(UnitId, 345, 54321, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine("单个寄存器写入完成");

            // 测试读取单个寄存器
            output.WriteLine("开始读取单个寄存器，地址345");
            var singleRegister2 = await client.ReadHoldingRegistersAsync(UnitId, 345, 1, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"单个寄存器读取完成，值: {singleRegister2[0]}");

            // 验证结果
            Assert.Equal(12345, singleRegister[0]);
            Assert.Equal(54321, singleRegister2[0]);

            #endregion

            #region 测试多寄存器读写

            // 测试写入多个寄存器
            ushort[] writeValues = [100, 200, 300, 400, 500];
            output.WriteLine("开始写入多个寄存器，地址10");
            // 明确指定调用非泛型版本
            await client.WriteMultipleRegistersAsync(UnitId, 10, writeValues, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine("多个寄存器写入完成");

            // 测试读取多个寄存器
            output.WriteLine("开始读取多个寄存器，地址10");
            var multipleRegisters = await client.ReadHoldingRegistersAsync(UnitId, 10, 5, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"多个寄存器读取完成，值: {string.Join(", ", multipleRegisters)}");

            ushort[] writeValues2 = [600, 700, 800, 900, 1000, 1100, 1200];
            output.WriteLine("开始写入多个寄存器，地址456");
            // 明确指定调用非泛型版本
            await client.WriteMultipleRegistersAsync(UnitId, 456, writeValues2, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine("多个寄存器写入完成");

            // 测试读取多个寄存器
            output.WriteLine("开始读取多个寄存器，地址456");
            var multipleRegisters2 = await client.ReadHoldingRegistersAsync(UnitId, 456, 7, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"多个寄存器读取完成，值: {string.Join(", ", multipleRegisters2)}");

            // 验证结果
            Assert.Equal(writeValues, multipleRegisters);
            Assert.Equal(writeValues2, multipleRegisters2);

            #endregion
        } finally {
            // 清理资源
        }
    }

    [Fact]
    public async Task ModbusRtuOverTcp_GenericRegisters_Test() {
        try {
            // 启动FluentModbus服务器
            StartFluentModbusServer();

            var config = new NetworkConfig {
                RemoteHost = "127.0.0.1",
                RemotePort = ServerPort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };
            ModbusLibClient client = new ModbusTcpClient(config, ProtocolType.Rtu);
            var isConnected = await client.ConnectAsync(TestContext.Current.CancellationToken);
            Assert.True(isConnected, "客户端连接失败");

            //var fclient = new ModbusTcpClient();
            //fclient.Connect(new IPEndPoint(IPAddress.Loopback, ServerPort), ModbusEndianness.BigEndian);
            //Assert.True(fclient.IsConnected, "Fluent客户端连接失败");

            #region 测试单寄存器读写

            await client.WriteSingleRegisterAsync(UnitId, 0, 12345, cancelToken: TestContext.Current.CancellationToken);
            var value1 = (await client.ReadHoldingRegistersAsync(UnitId, 0, 1, cancelToken: TestContext.Current.CancellationToken))[0];
            //var valuef1 = fclient.ReadHoldingRegisters<ushort>(UnitId, 0, 1)[0];
            Assert.Equal(12345, value1);
            //Assert.Equal(12345, valuef1);

            await client.WriteSingleRegisterAsync(UnitId, 345, -5421, cancelToken: TestContext.Current.CancellationToken);
            var value2 = (await client.ReadHoldingRegistersAsync<short>(UnitId, 345, 1, cancelToken: TestContext.Current.CancellationToken))[0];
            //var valuef2 = fclient.ReadHoldingRegisters<short>(UnitId, 345, 1)[0];
            Assert.Equal(-5421, value2);
            //Assert.Equal(-5421, valuef2);

            #endregion

            #region 测试浮点数数组读写

            float[] src3 = [1.23f, 4.56f, 7.89f];
            await client.WriteMultipleRegistersAsync(UnitId, 0, src3, cancelToken: TestContext.Current.CancellationToken);
            var value3 = await client.ReadHoldingRegistersAsync<float>(UnitId, 0, 3, cancelToken: TestContext.Current.CancellationToken);
            //var valuef3 = fclient.ReadHoldingRegisters<float>(UnitId, 0, 3).ToArray();
            Assert.Equal(src3, value3);
            //Assert.Equal(src3, valuef3);

            float[] src4 = [9.87f, 6.54f, 3.21f, 1.23f];
            await client.WriteMultipleRegistersAsync(UnitId, 345, src4, cancelToken: TestContext.Current.CancellationToken);
            var value4 = await client.ReadHoldingRegistersAsync<float>(UnitId, 345, 4, cancelToken: TestContext.Current.CancellationToken);
            //var valuef4 = fclient.ReadHoldingRegisters<float>(UnitId, 345, 4).ToArray();
            Assert.Equal(src4, value4);
            //Assert.Equal(src4, valuef4);

            #endregion

            #region 测试双精度浮点数读写

            double src5 = 123.456;
            await client.WriteMultipleRegistersAsync(UnitId, 10, src5, cancelToken: TestContext.Current.CancellationToken);
            var value5 = await client.ReadHoldingRegistersAsync<double>(UnitId, 10, 1, cancelToken: TestContext.Current.CancellationToken);
            //var valuef5 = fclient.ReadHoldingRegisters<double>(UnitId, 10, 1);
            Assert.Equal(src5, value5[0]);
            //Assert.Equal(src5, valuef5[0]);

            double[] src6 = [654.321, -234.567, 1265374.234445];
            await client.WriteMultipleRegistersAsync(UnitId, 456, src6, cancelToken: TestContext.Current.CancellationToken);
            var value6 = await client.ReadHoldingRegistersAsync<double>(UnitId, 456, 3, cancelToken: TestContext.Current.CancellationToken);
            //var valuef6 = fclient.ReadHoldingRegisters<double>(UnitId, 456, 3).ToArray();
            Assert.Equal(src6, value6);
            //Assert.Equal(src6, valuef6);

            #endregion

            client.Disconnect();
            //fclient.Disconnect();
        } finally {
            // 清理资源
        }
    }

    [Fact]
    public async Task ModbusRtuOverTcp_RWMultiRegisters_Test() {
        try {
            // 启动FluentModbus服务器
            StartFluentModbusServer();
            output.WriteLine($"FluentModbus服务器已启动，端口: {ServerPort}");

            // 使用我们自己的RTU over TCP客户端连接到服务器
            var config = new NetworkConfig {
                RemoteHost = "127.0.0.1",
                RemotePort = ServerPort,
                ConnectTimeout = 5000,
                ReceiveTimeout = 5000,
                SendTimeout = 5000
            };

            ModbusLibClient client = new ModbusTcpClient(config, ProtocolType.Rtu);
            output.WriteLine("ModbusLib RTU over TCP客户端已创建");

            var isConnected = await client.ConnectAsync(TestContext.Current.CancellationToken);
            output.WriteLine($"客户端连接结果: {isConnected}");

            Assert.True(isConnected, "客户端连接失败");

            #region 测试小地址读写多个寄存器

            // 先写入一些数据用于读取
            output.WriteLine("开始写入初始数据，地址0");
            ushort[] initialValues = [1000, 2000, 3000];
            // 明确指定调用非泛型版本
            await client.WriteMultipleRegistersAsync(UnitId, 0, initialValues, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine("初始数据写入完成");

            // 准备要写入的数据
            ushort[] writeValues = [5000, 6000, 7000];

            // 执行读写多个寄存器操作
            output.WriteLine("开始执行读写多个寄存器操作，读取地址0，写入地址10");
            var readValues = await client.ReadWriteMultipleRegistersAsync(UnitId, 0, 3, 10, writeValues, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"读写多个寄存器操作完成，读取值: {string.Join(", ", readValues)}");

            // 验证读取的数据是否正确
            Assert.Equal(initialValues, readValues);

            // 验证写入的数据是否正确
            output.WriteLine("开始验证写入的数据，地址10");
            var verifyValues = await client.ReadHoldingRegistersAsync(UnitId, 10, 3, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"验证数据读取完成，值: {string.Join(", ", verifyValues)}");
            Assert.Equal(writeValues, verifyValues);

            #endregion

            #region 测试大地址读写多个寄存器

            // 先写入一些数据用于读取（大地址）
            output.WriteLine("开始写入初始数据，地址345");
            ushort[] initialValues2 = [11000, 12000, 13000, 14000];
            // 明确指定调用非泛型版本
            await client.WriteMultipleRegistersAsync(UnitId, 345, initialValues2, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine("初始数据写入完成");

            // 准备要写入的数据（大地址）
            ushort[] writeValues2 = [15000, 16000, 17000, 18000];

            // 执行读写多个寄存器操作（大地址）
            output.WriteLine("开始执行读写多个寄存器操作，读取地址345，写入地址456");
            var readValues2 = await client.ReadWriteMultipleRegistersAsync(UnitId, 345, 4, 456, writeValues2, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"读写多个寄存器操作完成，读取值: {string.Join(", ", readValues2)}");

            // 验证读取的数据是否正确
            Assert.Equal(initialValues2, readValues2);

            // 验证写入的数据是否正确
            output.WriteLine("开始验证写入的数据，地址456");
            var verifyValues2 = await client.ReadHoldingRegistersAsync(UnitId, 456, 4, cancelToken: TestContext.Current.CancellationToken);
            output.WriteLine($"验证数据读取完成，值: {string.Join(", ", verifyValues2)}");
            Assert.Equal(writeValues2, verifyValues2);

            #endregion
        } finally {
            // 清理资源
        }
    }

    private void StartFluentModbusServer() {
        if (_mbus_erver != null) {
            _mbus_erver.Stop();
            _mbus_erver.Dispose();
        }

        _mbus_erver = new ModbusRtuOverTcpServer();
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