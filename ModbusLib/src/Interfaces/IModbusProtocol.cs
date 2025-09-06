using ModbusLib.Models;

namespace ModbusLib.Interfaces;

/// <summary>
/// Modbus协议处理接口
/// </summary>
public interface IModbusProtocol
{
    /// <summary>
    /// 构建请求数据
    /// </summary>
    /// <param name="request">Modbus请求</param>
    /// <returns>请求字节数组</returns>
    byte[] BuildRequest(ModbusRequest request);

    /// <summary>
    /// 解析响应数据
    /// </summary>
    /// <param name="response">响应字节数组</param>
    /// <param name="request">原始请求</param>
    /// <returns>Modbus响应</returns>
    ModbusResponse ParseResponse(byte[] response, ModbusRequest request);

    /// <summary>
    /// 验证响应数据的完整性
    /// </summary>
    /// <param name="response">响应字节数组</param>
    /// <returns>是否有效</returns>
    bool ValidateResponse(byte[] response);

    /// <summary>
    /// 计算期望的响应长度
    /// </summary>
    /// <param name="request">请求</param>
    /// <returns>期望的响应长度</returns>
    int CalculateExpectedResponseLength(ModbusRequest request);
}