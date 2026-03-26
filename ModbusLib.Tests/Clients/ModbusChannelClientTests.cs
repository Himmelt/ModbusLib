using ModbusLib.Clients;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Tests.Clients;

public class ModbusChannelClientTests : IDisposable {

    private bool _disposed;
    private readonly ChannelSession _session;
    private readonly ModbusChannelClient _client;

    public ModbusChannelClientTests() {
        _session = new ChannelSession();
        _client = new ModbusChannelClient(_session, new TcpProtocol());
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
        using var client = ModbusChannelClient.CreateTcpClient(session);

        Assert.NotNull(client);
        Assert.True(client.IsConnected);

        client.Dispose();
    }

    [Fact]
    public void CreateRtuClient_WithValidSession_CreatesClient() {
        var session = new ChannelSession();
        using var client = ModbusChannelClient.CreateRtuClient(session);

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
    }

    [Fact]
    public void Constructor_WithProtocolAndTransport_InitializesCorrectly() {
        var protocol = new TcpProtocol();
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session, protocol, 2000);

        Assert.True(client.IsConnected);
        Assert.Equal(2000, client.Timeout);
    }

    [Fact]
    public void Constructor_WithProtocolAndTimeout_InitializesCorrectly() {
        var protocol = new RtuProtocol();
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session, protocol, 3000);

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
