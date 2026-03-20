using System.Runtime.CompilerServices;

namespace ModbusLib.Utils;

/// <summary>
/// CancellationToken扩展方法
/// </summary>
public static class CancellationTokenExtensions {
    /// <summary>
    /// 检查取消令牌是否已请求取消，如果已取消则抛出带有中文消息的 OperationCanceledException
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="message">自定义消息</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCancelRequestCN(this CancellationToken cancellationToken, string message = "操作已取消") {
        if (cancellationToken.IsCancellationRequested) {
            throw new OperationCanceledException(message);
        }
    }
}
