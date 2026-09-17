using System.Threading.Channels;

namespace ModbusLib.Models;

/// <summary>
/// 一次会话的双向通道容器：<see cref="ClientToServer"/> 承载请求，<see cref="ServerToClient"/> 承载响应。
/// </summary>
public class ChannelSession {

    /// <summary>
    /// 创建无界会话（默认）。无界通道不会丢弃任何帧，配对正确性完全由传输层负责
    /// （见 <c>ChannelTransport</c>：发请求前排空 + 响应相关性校验）。
    /// </summary>
    public ChannelSession() {
        ServerToClient = Channel.CreateUnbounded<byte[]>();
        ClientToServer = Channel.CreateUnbounded<byte[]>();
    }

    /// <summary>
    /// 创建会话，并把 <see cref="ServerToClient"/> 限定为容量 <paramref name="serverToClientCapacity"/> 的有界通道，
    /// 写满时丢弃最旧的滞留帧，保证新帧（可能正是本轮响应）仍能入队。
    /// 用于“对端持续灌帧、客户端不再消费”时兜底防内存无上限增长；它只限制滞留量，
    /// <b>不能</b>替代请求/响应配对正确性。
    /// </summary>
    /// <remarks>
    /// <see cref="ClientToServer"/> 保持无界：客户端由通信锁保证严格串行，在途请求最多 1 个。
    /// </remarks>
    public ChannelSession(int serverToClientCapacity) {
        ArgumentOutOfRangeException.ThrowIfLessThan(serverToClientCapacity, 1);

        ServerToClient = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(serverToClientCapacity) {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        ClientToServer = Channel.CreateUnbounded<byte[]>();
    }

    public Channel<byte[]> ServerToClient { get; }
    public Channel<byte[]> ClientToServer { get; }
}
