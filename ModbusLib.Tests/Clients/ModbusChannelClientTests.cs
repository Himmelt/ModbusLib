using ModbusLib.Clients;
using ModbusLib.Models;

namespace ModbusLib.Tests.Clients;

public class ModbusChannelClientTests : IDisposable {

    private bool _disposed;
    private readonly ChannelSession _session;
    private readonly ModbusChannelClient _client;

    public ModbusChannelClientTests() {
        _session = new ChannelSession();
        _client = new ModbusChannelClient(_session);
    }

    public void Dispose() {
        if (_disposed) return;
        _client.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void CreateTcpClient_WithValidSession_CreatesClient() {
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session);

        Assert.NotNull(client);
        Assert.True(client.IsConnected);

        client.Dispose();
    }

    [Fact]
    public void CreateRtuClient_WithValidSession_CreatesClient() {
        var session = new ChannelSession();
        using var client = new ModbusRtuOverChannelClient(session);

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