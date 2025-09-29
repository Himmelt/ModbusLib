using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ModbusLib.Enums;

/// <summary>
/// Modbus 异常码枚举
/// </summary>
[SuppressMessage("Design", "CA1027")]
[SuppressMessage("CodeQuality", "IDE0079")]
public enum ModbusExceptionCode {
    /// <summary>
    /// 仅由服务器使用，表示不应向客户端返回异常。
    /// </summary>
    [Description("无异常")]
    None = 0x00,

    /// <summary>
    /// 查询中接收到的功能码对服务器来说是不允许的操作。
    /// </summary>
    [Description("非法功能码")]
    IllegalFunction = 0x01,

    /// <summary>
    /// 查询中接收到的数据地址对服务器来说是不允许的地址。
    /// </summary>
    [Description("非法数据地址")]
    IllegalDataAddress = 0x02,

    /// <summary>
    /// 查询数据字段中包含的值对服务器来说是不允许的值。
    /// </summary>
    [Description("非法数据值")]
    IllegalDataValue = 0x03,

    /// <summary>
    /// 服务器在尝试执行请求的操作时发生不可恢复的错误。
    /// </summary>
    [Description("目标设备故障")]
    TargetDeviceFailure = 0x04,

    /// <summary>
    /// 与编程命令结合使用。服务器已接受请求并正在处理，但需要较长时间才能完成。
    /// </summary>
    [Description("确认")]
    ServerAcknowledge = 0x05,

    /// <summary>
    /// 与编程命令结合使用。正在处理长时间运行的程序命令。
    /// </summary>
    [Description("目标设备忙")]
    TargetDeviceBusy = 0x06,

    /// <summary>
    /// 负确认（无法执行编程功能）
    /// </summary>
    [Description("负确认")]
    NegativeAcknowledge = 0x07,

    /// <summary>
    /// 与功能码20和21及引用类型6结合使用，表示扩展文件区域未能通过一致性检查。
    /// </summary>
    [Description("内存校验错误")]
    MemoryParityError = 0x08,

    /// <summary>
    /// 与网关结合使用，表示网关无法为处理请求分配从输入端口到输出端口的内部通信路径。
    /// </summary>
    [Description("网关路径不可用")]
    GatewayPathUnavailable = 0x0A,

    /// <summary>
    /// 与网关结合使用，表示未从目标设备获得响应。
    /// </summary>
    [Description("网关目标设备无响应")]
    GatewayTargetDeviceFailedToRespond = 0x0B
}

public static class ModbusExceptionCodeDescriptions {

    private static readonly Dictionary<ModbusExceptionCode, string> Descriptions = [];

    static ModbusExceptionCodeDescriptions() {
        var type = typeof(ModbusExceptionCode);

        foreach (var code in Enum.GetValues<ModbusExceptionCode>()) {
            var field = type.GetField(code.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            Descriptions[code] = attribute?.Description ?? code.ToString();
        }
    }

    public static string GetDescription(this ModbusExceptionCode exCode) {
        return Descriptions.TryGetValue(exCode, out var description) ? description : "未知错误";
    }
}
