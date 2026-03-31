using System.IO.Pipelines;

namespace ModbusLib.Models;

public class PipeSession {
    public Pipe ServerToClient { get; } = new Pipe();
    public Pipe ClientToServer { get; } = new Pipe();
}