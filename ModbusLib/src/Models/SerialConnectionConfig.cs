using System.IO.Ports;

namespace ModbusLib.Models;

/// <summary>
/// 串口连接配置
/// </summary>
public class SerialConnectionConfig
{
    /// <summary>
    /// 串口名称 (如 COM1, COM2)
    /// </summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>
    /// 波特率
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 校验位
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 数据位
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// 握手协议
    /// </summary>
    public Handshake Handshake { get; set; } = Handshake.None;

    /// <summary>
    /// 读取超时时间（毫秒）
    /// </summary>
    public int ReadTimeout { get; set; } = 1000;

    /// <summary>
    /// 写入超时时间（毫秒）
    /// </summary>
    public int WriteTimeout { get; set; } = 1000;

    /// <summary>
    /// 字符间隔超时时间（毫秒）
    /// </summary>
    public int InterCharTimeout { get; set; } = 50;
}