using ModbusLib.Clients;
using ModbusLib.Protocols;
using ModbusLib.Transports;
using System.IO.Pipelines;

namespace ModbusLib.Tests.Clients;

public class ModbusPipeClientTests : IDisposable {
    private readonly Pipe _pipeIn;
    private readonly Pipe _pipeOut;
    private readonly PipeTransport _transport;
    private readonly ModbusPipeClient _client;
    private bool _disposed;

    public ModbusPipeClientTests() {
        _pipeIn = new Pipe();
        _pipeOut = new Pipe();
        _transport = new PipeTransport(_pipeIn, _pipeOut, 5000);
        _client = new ModbusPipeClient(_pipeIn, _pipeOut, new TcpProtocol(), _transport);
    }

    public void Dispose() {
        if (_disposed) return;
        _client.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CreateTcpClient_WithValidPipes_CreatesClient() {
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        using var client = ModbusPipeClient.CreateTcpClient(pipeIn, pipeOut);

        Assert.NotNull(client);
        Assert.True(client.IsConnected);

        client.Dispose();
    }

    [Fact]
    public void CreateRtuClient_WithValidPipes_CreatesClient() {
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        using var client = ModbusPipeClient.CreateRtuClient(pipeIn, pipeOut);

        Assert.NotNull(client);
        Assert.True(client.IsConnected);

        client.Dispose();
    }

    [Fact]
    public void IsConnected_WithValidTransport_ReturnsTrue() {
        Assert.True(_client.IsConnected);
    }

    [Fact]
    public void Timeout_SetValue_ReflectsInTransport() {
        _client.Timeout = 3000;
        Assert.Equal(3000, _client.Timeout);
        Assert.Equal(3000, _transport.Timeout);
    }

    [Fact]
    public void Constructor_WithProtocolAndTransport_InitializesCorrectly() {
        var protocol = new TcpProtocol();
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        using var transport = new PipeTransport(pipeIn, pipeOut, 2000);
        using var client = new ModbusPipeClient(pipeIn, pipeOut, protocol, transport);

        Assert.True(client.IsConnected);
        Assert.Equal(2000, client.Timeout);
    }

    [Fact]
    public void Constructor_WithProtocolAndTimeout_InitializesCorrectly() {
        var protocol = new RtuProtocol();
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        using var client = new ModbusPipeClient(pipeIn, pipeOut, protocol, 3000);

        Assert.True(client.IsConnected);
        Assert.Equal(3000, client.Timeout);
    }

    [Fact]
    public void Dispose_AfterMultipleDispose_DoesNotThrow() {
        _client.Dispose();
        _client.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_ReturnsTrue() {
        var result = await _client.ConnectAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task DisconnectAsync_CompletesSuccessfully() {
        await _client.DisconnectAsync();
        Assert.True(_client.IsConnected);
    }
}