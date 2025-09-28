using System.Net;
using FluentModbus;
using ModbusLib.Factories;
using ModbusLib.Models;
using ModbusLib.Transports;
using Xunit;
using Xunit.Abstractions;

namespace ModbusLib.Tests.Functional;

/// <summary>
/// 连接状态变化测试
/// </summary>
public class ConnectionStateChangeTest
{
    private readonly ITestOutputHelper _output;

    public ConnectionStateChangeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试连接状态在请求前后的变化
    /// </summary>
    [Fact]
    public async Task TestConnectionStateBeforeAndAfterRequest()
    {
        const int port = 510;
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
        _output.WriteLine($"连接结果: {isConnected}");
        _output.WriteLine($"连接后状态详情: {transport.GetConnectionStateDetails()}");

        // 等待一小段时间
        await Task.Delay(100);
        _output.WriteLine($"延迟后状态详情: {transport.GetConnectionStateDetails()}");

        // 尝试发送一个简单的请求
        try
        {
            // 构造一个简单的Modbus请求（功能码3，读取寄存器）
            var request = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 };
            _output.WriteLine($"发送请求前状态详情: {transport.GetConnectionStateDetails()}");
            
            var response = await transport.SendReceiveAsync(request);
            _output.WriteLine($"请求完成，响应长度: {response.Length}");
            _output.WriteLine($"请求后状态详情: {transport.GetConnectionStateDetails()}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"发送请求时出错: {ex.Message}");
            _output.WriteLine($"错误时状态详情: {transport.GetConnectionStateDetails()}");
        }
    }
}