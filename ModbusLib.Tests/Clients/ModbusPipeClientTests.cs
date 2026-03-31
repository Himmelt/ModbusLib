using ModbusLib.Clients;
using ModbusLib.Models;

namespace ModbusLib.Tests.Clients;

public class ModbusPipeClientTests : IDisposable {

    private bool _disposed;
    private readonly PipeSession session;
    private readonly ModbusPipeClient _client;

    public ModbusPipeClientTests() {
        session = new PipeSession();
        _client = new ModbusPipeClient(session);
    }

    public void Dispose() {
        if (_disposed) return;
        _client.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CreateTcpClient_WithValidPipes_CreatesClient() {
        PipeSession _session = new PipeSession();
        using ModbusPipeClient client = new ModbusPipeClient(_session);

        Assert.NotNull(client);
        Assert.True(client.IsConnected);

        client.Dispose();
    }

    [Fact]
    public void CreateRtuClient_WithValidPipes_CreatesClient() {
        PipeSession _session = new PipeSession();
        using var client = new ModbusRtuOverPipeClient(_session);

        Assert.NotNull(client);
        Assert.True(client.IsConnected);

        client.Dispose();
    }

    [Fact]
    public void IsConnected_WithValidTransport_ReturnsTrue() {
        Assert.True(_client.IsConnected);
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