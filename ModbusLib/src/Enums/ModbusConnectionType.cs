using System.IO.Ports;

namespace ModbusLib.Enums;

/// <summary>
/// 连接类型枚举
/// </summary>
public enum ModbusConnectionType
{
    /// <summary>
    /// RTU 串口连接
    /// </summary>
    Rtu,

    /// <summary>
    /// TCP 网络连接
    /// </summary>
    Tcp,

    /// <summary>
    /// UDP 网络连接
    /// </summary>
    Udp,

    /// <summary>
    /// RTU over TCP 连接
    /// </summary>
    RtuOverTcp,

    /// <summary>
    /// RTU over UDP 连接
    /// </summary>
    RtuOverUdp
}