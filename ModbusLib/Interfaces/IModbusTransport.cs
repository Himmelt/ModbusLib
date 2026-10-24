namespace ModbusLib.Interfaces;

using ModbusLib.Models;

/// <summary>
/// Modbus传输层接口
/// </summary>
public interface IModbusTransport : IDisposable, IAsyncDisposable {
    /// <summary>
    /// 异步连接
    /// </summary>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns>连接是否成功</returns>
    Task<bool> ConnectAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// 异步断开连接
    /// </summary>
    /// <param name="cancelToken">取消令牌</param>
    Task DisconnectAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// 获取连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 链路健康状态。传输层只在拿到**明确证据**（FIN/RST/IO 异常）时才翻转为 <see cref="LinkState.Down"/>；
    /// "请求超时但没有错误信号"不算链路断开，状态保持 <see cref="LinkState.Up"/>。
    /// </summary>
    LinkState LinkState { get; }

    /// <summary>
    /// 链路状态变更通知。连接被传输层关闭时会触发，调用方无需轮询。
    /// </summary>
    event EventHandler<LinkStateChangedEventArgs>? LinkStateChanged;

    /// <summary>
    /// 发送请求并接收响应
    /// </summary>
    /// <param name="request">请求数据</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <returns>响应数据</returns>
    Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default);

    /// <summary>
    /// 超时时间（单位：毫秒，ms）
    /// 小于0表示忽略超时时间设置
    /// </summary>
    int Timeout { get; set; }
}
