namespace ModbusLib.Models;

/// <summary>
/// 网络连接配置
/// </summary>
public class NetworkConfig {
    /// <summary>
    /// 远程主机地址（IP地址或域名）
    /// </summary>
    public string RemoteHost { get; set; } = "localhost";

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
    /// 本地端口号（可选，未指定时使用系统分配的本地端口）
    /// </summary>
    public int? LocalPort { get; set; }

    /// <summary>
    /// 等待通信锁（请求串行化）的超时时间（毫秒）。-1 表示沿用 <see cref="Timeout"/>。
    /// <para>
    /// 等锁与收发是两件不同的事：前者只说明"这条链路还被别人占着"，后者才说明"对端回没回"。
    /// 分开配置后，共享一条连接的多设备之间排队不会挤占真正的通信预算。
    /// </para>
    /// </summary>
    public int LockTimeout { get; set; } = -1;

    /// <summary>
    /// 是否启用 TCP KeepAlive。默认启用。
    /// </summary>
    public bool KeepAliveEnabled { get; set; } = true;

    /// <summary>
    /// KeepAlive 静默多久后发出第一个探测包（秒）。默认 30 秒。
    /// <para>操作系统默认 2 小时，对"对端静默消失（掉电/拔线/会话被清）"毫无帮助。</para>
    /// </summary>
    public int KeepAliveTimeSeconds { get; set; } = 30;

    /// <summary>
    /// KeepAlive 探测包之间的间隔（秒）。默认 5 秒。
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// KeepAlive 无响应的重试次数，超过则内核判定连接已死。默认 3 次。
    /// </summary>
    public int KeepAliveRetryCount { get; set; } = 3;
}
