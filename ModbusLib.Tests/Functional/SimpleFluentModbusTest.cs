using System.Net;
using FluentModbus;
using Xunit;
using Xunit.Abstractions;

namespace ModbusLib.Tests.Functional;

/// <summary>
/// 简单的FluentModbus测试，验证从机是否能正常工作
/// </summary>
public class SimpleFluentModbusTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private const int SlavePort = 505; // 使用505端口避免冲突
    private ModbusTcpServer? _slaveServer;
    private bool _disposed;

    public SimpleFluentModbusTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// 测试FluentModbus从机是否能正常启动
    /// </summary>
    [Fact]
    public void TestFluentModbusSlaveStart()
    {
        // 启动FluentModbus从机
        StartFluentModbusSlave();
        
        // 验证服务器已启动
        Assert.NotNull(_slaveServer);
        _output.WriteLine("FluentModbus从机启动成功");
    }

    /// <summary>
    /// 启动FluentModbus TCP从机
    /// </summary>
    private void StartFluentModbusSlave()
    {
        if (_slaveServer != null)
        {
            _slaveServer.Stop();
            _slaveServer.Dispose();
        }

        _slaveServer = new ModbusTcpServer();
        _slaveServer.Start(new IPEndPoint(IPAddress.Loopback, SlavePort));
        
        // 等待服务器启动
        System.Threading.Thread.Sleep(100);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _slaveServer?.Stop();
                _slaveServer?.Dispose();
            }

            _disposed = true;
        }
    }
}