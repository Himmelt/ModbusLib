namespace ModbusLib.Interfaces;

/// <summary>
/// Modbus传输层接口
/// </summary>
public interface IModbusTransport : IDisposable
{
    /// <summary>
    /// 异步连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否成功</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步断开连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 发送请求并接收响应
    /// </summary>
    /// <param name="request">请求数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应数据</returns>
    Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 超时时间
    /// </summary>
    TimeSpan Timeout { get; set; }
}