using System.IO.Ports;
using ModbusLib.Models;

namespace ModbusLib.Factories;

/// <summary>
/// 串口连接配置构建器
/// </summary>
public class SerialConnectionConfigBuilder
{
    private readonly SerialConnectionConfig _config = new();

    public SerialConnectionConfigBuilder PortName(string portName)
    {
        _config.PortName = portName;
        return this;
    }

    public SerialConnectionConfigBuilder BaudRate(int baudRate)
    {
        _config.BaudRate = baudRate;
        return this;
    }

    public SerialConnectionConfigBuilder Parity(Parity parity)
    {
        _config.Parity = parity;
        return this;
    }

    public SerialConnectionConfigBuilder DataBits(int dataBits)
    {
        _config.DataBits = dataBits;
        return this;
    }

    public SerialConnectionConfigBuilder StopBits(StopBits stopBits)
    {
        _config.StopBits = stopBits;
        return this;
    }

    public SerialConnectionConfigBuilder Handshake(Handshake handshake)
    {
        _config.Handshake = handshake;
        return this;
    }

    public SerialConnectionConfigBuilder ReadTimeout(int timeout)
    {
        _config.ReadTimeout = timeout;
        return this;
    }

    public SerialConnectionConfigBuilder WriteTimeout(int timeout)
    {
        _config.WriteTimeout = timeout;
        return this;
    }

    public SerialConnectionConfigBuilder InterCharTimeout(int timeout)
    {
        _config.InterCharTimeout = timeout;
        return this;
    }

    public SerialConnectionConfig Build()
    {
        return _config;
    }

    /// <summary>
    /// 创建默认RTU配置
    /// </summary>
    public static SerialConnectionConfigBuilder DefaultRtu()
    {
        return new SerialConnectionConfigBuilder()
            .BaudRate(9600)
            .Parity(System.IO.Ports.Parity.None)
            .DataBits(8)
            .StopBits(System.IO.Ports.StopBits.One)
            .Handshake(System.IO.Ports.Handshake.None);
    }
}

/// <summary>
/// 网络连接配置构建器
/// </summary>
public class NetworkConnectionConfigBuilder
{
    private readonly NetworkConnectionConfig _config = new();

    public NetworkConnectionConfigBuilder Host(string host)
    {
        _config.Host = host;
        return this;
    }

    public NetworkConnectionConfigBuilder Port(int port)
    {
        _config.Port = port;
        return this;
    }

    public NetworkConnectionConfigBuilder ConnectTimeout(int timeout)
    {
        _config.ConnectTimeout = timeout;
        return this;
    }

    public NetworkConnectionConfigBuilder ReceiveTimeout(int timeout)
    {
        _config.ReceiveTimeout = timeout;
        return this;
    }

    public NetworkConnectionConfigBuilder SendTimeout(int timeout)
    {
        _config.SendTimeout = timeout;
        return this;
    }

    public NetworkConnectionConfigBuilder KeepAlive(bool keepAlive)
    {
        _config.KeepAlive = keepAlive;
        return this;
    }

    public NetworkConnectionConfigBuilder NoDelay(bool noDelay)
    {
        _config.NoDelay = noDelay;
        return this;
    }

    public NetworkConnectionConfigBuilder ReceiveBufferSize(int size)
    {
        _config.ReceiveBufferSize = size;
        return this;
    }

    public NetworkConnectionConfigBuilder SendBufferSize(int size)
    {
        _config.SendBufferSize = size;
        return this;
    }

    public NetworkConnectionConfig Build()
    {
        return _config;
    }

    /// <summary>
    /// 创建默认TCP配置
    /// </summary>
    public static NetworkConnectionConfigBuilder DefaultTcp()
    {
        return new NetworkConnectionConfigBuilder()
            .Port(502)
            .ConnectTimeout(10000)
            .ReceiveTimeout(5000)
            .SendTimeout(5000)
            .KeepAlive(true)
            .NoDelay(true);
    }

    /// <summary>
    /// 创建默认UDP配置
    /// </summary>
    public static NetworkConnectionConfigBuilder DefaultUdp()
    {
        return new NetworkConnectionConfigBuilder()
            .Port(502)
            .ReceiveTimeout(5000)
            .SendTimeout(5000);
    }
}