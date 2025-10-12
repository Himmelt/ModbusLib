using ModbusLib.Clients;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Factories;

/// <summary>
/// Modbus客户端工厂
/// </summary>
public static class ModbusClientFactory {
    /// <summary>
    /// 创建RTU客户端
    /// </summary>
    /// <param name="config">串口连接配置</param>
    /// <returns>RTU客户端</returns>
    public static IModbusClient CreateRtuClient(SerialConnectionConfig config) {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        return new ModbusRtuClient(config);
    }

    /// <summary>
    /// 创建RTU客户端（使用默认配置）
    /// </summary>
    /// <param name="portName">串口名称</param>
    /// <param name="baudRate">波特率</param>
    /// <returns>RTU客户端</returns>
    public static IModbusClient CreateRtuClient(string portName, int baudRate = 9600) {
        if (string.IsNullOrEmpty(portName))
            throw new ArgumentException("串口名称不能为空", nameof(portName));

        var config = new SerialConnectionConfig {
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
    public static IModbusClient CreateTcpClient(NetworkConnectionConfig config) {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        return new ModbusTcpClient(config);
    }

    /// <summary>
    /// 创建TCP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>TCP客户端</returns>
    public static IModbusClient CreateTcpClient(string host, int port = 502) {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig {
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
    public static IModbusClient CreateUdpClient(NetworkConnectionConfig config) {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        return new ModbusUdpClient(config);
    }

    /// <summary>
    /// 创建UDP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>UDP客户端</returns>
    public static IModbusClient CreateUdpClient(string host, int port = 502) {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig {
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
    public static IModbusClient CreateRtuOverTcpClient(NetworkConnectionConfig config) {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        return new ModbusRtuOverTcpClient(config);
    }

    /// <summary>
    /// 创建RTU over TCP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>RTU over TCP客户端</returns>
    public static IModbusClient CreateRtuOverTcpClient(string host, int port = 502) {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig {
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
    public static IModbusClient CreateRtuOverUdpClient(NetworkConnectionConfig config) {
        ArgumentNullException.ThrowIfNull(config, nameof(config));

        return new ModbusRtuOverUdpClient(config);
    }

    /// <summary>
    /// 创建RTU over UDP客户端（使用默认配置）
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口号</param>
    /// <returns>RTU over UDP客户端</returns>
    public static IModbusClient CreateRtuOverUdpClient(string host, int port = 502) {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentException("主机地址不能为空", nameof(host));

        var config = new NetworkConnectionConfig {
            Host = host,
            Port = port
        };

        return new ModbusRtuOverUdpClient(config);
    }
}