using System.Net;
using System.Net.Sockets;
using FluentModbus;
using ModbusLib.Factories;
using ModbusLib.Models;
using ModbusLib.Transports;
using Xunit;
using Xunit.Abstractions;
using ModbusLibClient = ModbusLib.Interfaces.IModbusClient;

namespace ModbusLib.Tests.Functional;

/// <summary>
/// 调试连接问题的测试
/// </summary>
public class DebugConnectionTest
{
    private readonly ITestOutputHelper _output;

    public DebugConnectionTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试TCP连接的基本功能
    /// </summary>
    [Fact]
    public async Task TestTcpConnection()
    {
        const int port = 507;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        // 启动FluentModbus服务器
        using var server = new ModbusTcpServer();
        server.Start(endpoint);
        _output.WriteLine("FluentModbus服务器已启动");

        // 等待服务器启动
        await Task.Delay(100);

        // 测试直接TCP连接
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", port);
        _output.WriteLine($"TCP客户端连接状态: {tcpClient.Connected}");

        tcpClient.Close();
    }

    /// <summary>
    /// 测试ModbusLib TcpTransport连接
    /// </summary>
    [Fact]
    public async Task TestModbusLibTcpTransport()
    {
        const int port = 508;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        // 启动FluentModbus服务器
        using var server = new ModbusTcpServer();
        server.Start(endpoint);
        _output.WriteLine("FluentModbus服务器已启动");

        // 等待服务器启动
        await Task.Delay(100);

        // 测试ModbusLib TcpTransport
        var config = new NetworkConnectionConfig
        {
            Host = "127.0.0.1",
            Port = port,
            ConnectTimeout = 5000,
            ReceiveTimeout = 5000,
            SendTimeout = 5000
        };

        using var transport = new TcpTransport(config);
        _output.WriteLine("TcpTransport已创建");

        var isConnected = await transport.ConnectAsync();
        _output.WriteLine($"TcpTransport连接结果: {isConnected}");
        _output.WriteLine($"TcpTransport.IsConnected: {transport.IsConnected}");

        Assert.True(isConnected);
        Assert.True(transport.IsConnected);
    }
}