namespace ModbusLib.Models;

/// <summary>
/// 网络连接配置
/// </summary>
public class NetworkConnectionConfig
{
    /// <summary>
    /// 主机地址（IP地址或域名）
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; } = 502;

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 10000;

    /// <summary>
    /// 接收超时时间（毫秒）
    /// </summary>
    public int ReceiveTimeout { get; set; } = 5000;

    /// <summary>
    /// 发送超时时间（毫秒）
    /// </summary>
    public int SendTimeout { get; set; } = 5000;

    /// <summary>
    /// 是否启用 KeepAlive（仅适用于TCP）
    /// </summary>
    public bool KeepAlive { get; set; } = true;

    /// <summary>
    /// TCP NoDelay 选项（仅适用于TCP）
    /// </summary>
    public bool NoDelay { get; set; } = true;

    /// <summary>
    /// 接收缓冲区大小
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 8192;

    /// <summary>
    /// 发送缓冲区大小
    /// </summary>
    public int SendBufferSize { get; set; } = 8192;
}