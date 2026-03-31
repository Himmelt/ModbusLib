using System.Threading.Channels;

namespace ModbusLib.Models;

public class ChannelSession {
    public Channel<byte[]> ServerToClient { get; } = Channel.CreateUnbounded<byte[]>();
    public Channel<byte[]> ClientToServer { get; } = Channel.CreateUnbounded<byte[]>();
}