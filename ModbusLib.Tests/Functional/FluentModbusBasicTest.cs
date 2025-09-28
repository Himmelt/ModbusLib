using System.Net;
using FluentModbus;
using Xunit;
using Xunit.Abstractions;

namespace ModbusLib.Tests.Functional;

/// <summary>
/// FluentModbus基本功能测试
/// </summary>
public class FluentModbusBasicTest
{
    private readonly ITestOutputHelper _output;

    public FluentModbusBasicTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试FluentModbus服务器和客户端的基本通信
    /// </summary>
    [Fact]
    public void TestFluentModbusBasicCommunication()
    {
        const int port = 506;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);

        // 启动服务器
        using var server = new ModbusTcpServer();
        server.Start(endpoint);
        
        _output.WriteLine("服务器已启动");

        // 创建客户端
        using var client = new ModbusTcpClient();
        client.Connect(endpoint);
        
        _output.WriteLine("客户端已连接");

        // 测试写入和读取寄存器
        client.WriteSingleRegister(0, 0, 12345);
        var value = client.ReadHoldingRegisters<ushort>(0, 0, 1);
        
        _output.WriteLine($"读取到的值: {value[0]}");

        // 验证结果
        Assert.Equal(12345, value[0]);
        
        client.Disconnect();
    }
}