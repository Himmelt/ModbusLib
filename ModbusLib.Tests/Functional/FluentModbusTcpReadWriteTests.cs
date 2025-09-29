using FluentModbus;
using ModbusLib.Factories;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Tests.Functional;

public class FluentModbusTcpReadWriteTests {

    [Fact]
    public void TestReadCoil() {
/*        // 创建 FluentModbus 从机
        const int port = 5001;
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        // 启动服务器
        using var server = new ModbusTcpServer();
        server.AddUnit(1);
        server.Start(endpoint);*/

        // 创建 ModbusTcp 客户端
        using var client = ModbusClientFactory.CreateTcpClient("localhost", 502);
        client.Connect();
        client.WriteSingleCoil(1, 0, true);
        var value = client.ReadCoils(1, 0, 1);
        // 验证结果
        Assert.True(value[0]);
        client.Disconnect();
    }

    [Fact]
    public void TestUdpReadCoil() {

        using var client = ModbusClientFactory.CreateUdpClient("localhost", 666);
        client.Connect();
        client.WriteSingleCoil(1, 0, true);
        var value = client.ReadCoils(1, 0, 1);
        // 验证结果
        Assert.True(value[0]);
        client.Disconnect();
    }
}
