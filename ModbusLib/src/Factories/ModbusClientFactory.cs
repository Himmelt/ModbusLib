using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Factories;

/// <summary>
/// Modbus客户端工厂
/// </summary>
public static class ModbusClientFactory
{
    /// <summary>
    /// 创建RTU客户端
    /// </summary>
    /// <param name="config">串口连接配置</param>
    /// <returns>RTU客户端</returns>
    public static IModbusClient CreateRtuClient(SerialConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ModbusRtuClient(config);
    }

    /// <summary>
    /// 创建RTU客户端（使用默认配置）
    /// </summary>
    /// <param name="portName">串口名称</param>
    /// <param name="baudRate">波特率</param>
    /// <returns>RTU客户端</returns>
    public static IModbusClient CreateRtuClient(string portName, int baudRate = 9600)
    {
        if (string.IsNullOrEmpty(portName))
            throw new ArgumentException("串口名称不能为空", nameof(portName));

        var config = new SerialConnectionConfig
        {
            PortName = portName,
            BaudRate = baudRate
        };

        return new ModbusRtuClient(config);
    }

    /// <summary>
    /// 创建TCP客户端
    /// </summary>
    /// <param name="config">网络连接配置</param>
    /// <returns>TCP客户端</returns>
    public static IModbusClient CreateTcpClient(NetworkConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ModbusTcpClient(config);
    }

    /// <summary>
    /// 创建TCP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>TCP客户端</returns>
    public static IModbusClient CreateTcpClient(string host, int port = 502)
    {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig
        {
            Host = host,
            Port = port
        };

        return new ModbusTcpClient(config);
    }

    /// <summary>
    /// 创建UDP客户端
    /// </summary>
    /// <param name="config">网络连接配置</param>
    /// <returns>UDP客户端</returns>
    public static IModbusClient CreateUdpClient(NetworkConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ModbusUdpClient(config);
    }

    /// <summary>
    /// 创建UDP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>UDP客户端</returns>
    public static IModbusClient CreateUdpClient(string host, int port = 502)
    {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig
        {
            Host = host,
            Port = port
        };

        return new ModbusUdpClient(config);
    }

    /// <summary>
    /// 创建RTU over TCP客户端
    /// </summary>
    /// <param name="config">网络连接配置</param>
    /// <returns>RTU over TCP客户端</returns>
    public static IModbusClient CreateRtuOverTcpClient(NetworkConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ModbusRtuOverTcpClient(config);
    }

    /// <summary>
    /// 创建RTU over TCP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>RTU over TCP客户端</returns>
    public static IModbusClient CreateRtuOverTcpClient(string host, int port = 502)
    {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig
        {
            Host = host,
            Port = port
        };

        return new ModbusRtuOverTcpClient(config);
    }

    /// <summary>
    /// 创建RTU over UDP客户端
    /// </summary>
    /// <param name="config">网络连接配置</param>
    /// <returns>RTU over UDP客户端</returns>
    public static IModbusClient CreateRtuOverUdpClient(NetworkConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ModbusRtuOverUdpClient(config);
    }

    /// <summary>
    /// 创建RTU over UDP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>RTU over UDP客户端</returns>
    public static IModbusClient CreateRtuOverUdpClient(string host, int port = 502)
    {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig
        {
            Host = host,
            Port = port
        };

        return new ModbusRtuOverUdpClient(config);
    }

    /// <summary>
    /// 根据连接类型创建客户端
    /// </summary>
    /// <param name="connectionType">连接类型</param>
    /// <param name="serialConfig">串口配置（RTU时需要）</param>
    /// <param name="networkConfig">网络配置（网络连接时需要）</param>
    /// <returns>Modbus客户端</returns>
    public static IModbusClient CreateClient(ModbusConnectionType connectionType, 
        SerialConnectionConfig? serialConfig = null, 
        NetworkConnectionConfig? networkConfig = null)
    {
        return connectionType switch
        {
            ModbusConnectionType.Rtu => CreateRtuClient(serialConfig ?? throw new ArgumentNullException(nameof(serialConfig))),
            ModbusConnectionType.Tcp => CreateTcpClient(networkConfig ?? throw new ArgumentNullException(nameof(networkConfig))),
            ModbusConnectionType.Udp => CreateUdpClient(networkConfig ?? throw new ArgumentNullException(nameof(networkConfig))),
            ModbusConnectionType.RtuOverTcp => CreateRtuOverTcpClient(networkConfig ?? throw new ArgumentNullException(nameof(networkConfig))),
            ModbusConnectionType.RtuOverUdp => CreateRtuOverUdpClient(networkConfig ?? throw new ArgumentNullException(nameof(networkConfig))),
            _ => throw new NotSupportedException($"不支持的连接类型: {connectionType}")
        };
    }
}