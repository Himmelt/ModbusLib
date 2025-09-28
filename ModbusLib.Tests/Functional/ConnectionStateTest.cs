using System.Net;
using FluentModbus;
using ModbusLib.Factories;
using ModbusLib.Models;
using ModbusLib.Transports;
using Xunit;
using Xunit.Abstractions;

namespace ModbusLib.Tests.Functional;

/// <summary>
/// 连接状态测试
/// </summary>
public class ConnectionStateTest
{
    private readonly ITestOutputHelper _output;

    public ConnectionStateTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试连接状态的详细信息
    /// </summary>
    [Fact]
    public async Task TestConnectionStateDetails()
    {
        const int port = 509;
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

        // 检查初始状态
        _output.WriteLine($"初始连接状态: {transport.IsConnected}");

        var isConnected = await transport.ConnectAsync();
        _output.WriteLine($"连接结果: {isConnected}");
        
        // 检查连接后状态
        _output.WriteLine($"连接后状态: {transport.IsConnected}");
        if (transport is TcpTransport tcpTransport)
        {
            // 这里我们无法直接访问私有字段，但可以通过反射或修改代码来检查
            _output.WriteLine("连接已完成");
        }

        // 等待一小段时间
        await Task.Delay(100);
        
        // 再次检查状态
        _output.WriteLine($"延迟后状态: {transport.IsConnected}");

        // 尝试断开连接
        await transport.DisconnectAsync();
        _output.WriteLine($"断开连接后状态: {transport.IsConnected}");
    }
}