namespace ModbusLib.Models;

/// <summary>
/// 网络连接配置
/// </summary>
public class NetworkConfig {
    /// <summary>
    /// 主机地址（IP地址或域名）
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 远程端口号
    /// </summary>
    public int RemotePort { get; set; } = 502;

    /// <summary>
    /// 连接超时时间（毫秒），默认-1表示不启用连接超时
    /// </summary>
    public int ConnectTimeout { get; set; } = -1;

    /// <summary>
    /// 接收超时时间（毫秒），默认-1表示不启用接收超时
    /// </summary>
    public int ReceiveTimeout { get; set; } = -1;

    /// <summary>
    /// 发送超时时间（毫秒），默认-1表示不启用发送超时
    /// </summary>
    public int SendTimeout { get; set; } = -1;

    /// <summary>
    /// 接收缓冲区大小
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 8192;

    /// <summary>
    /// 发送缓冲区大小
    /// </summary>
    public int SendBufferSize { get; set; } = 8192;

    /// <summary>
    /// 本地主机地址（可选，未指定时使用系统默认的本地地址）
    /// </summary>
    public string? LocalHost { get; set; }

    /// <summary>
    /// 本地端口号（可选，未指定时使用系统默认分配的端口）
    /// </summary>
    public int? LocalPort { get; set; }
}