using System.IO.Ports;

namespace ModbusLib.Models;

/// <summary>
/// 串口连接配置
/// </summary>
public class SerialConfig {
    /// <summary>
    /// 串口名称 (如 COM1, COM2)
    /// </summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>
    /// 波特率，默认9600
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 校验位，默认None
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 数据位，默认8
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位，默认One
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// 握手协议，默认None表示不使用握手协议
    /// </summary>
    public Handshake Handshake { get; set; } = Handshake.None;

    /// <summary>
    /// 读取超时时间（毫秒），默认-1表示不启用
    /// </summary>
    public int ReadTimeout { get; set; } = -1;

    /// <summary>
    /// 写入超时时间（毫秒），默认-1表示不启用
    /// </summary>
    public int WriteTimeout { get; set; } = -1;

    /// <summary>
    /// 字符间隔超时时间（毫秒），默认-1表示不启用
    /// </summary>
    public int InterCharTimeout { get; set; } = -1;
}