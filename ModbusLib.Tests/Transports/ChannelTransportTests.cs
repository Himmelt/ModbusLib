using ModbusLib.Models;
using ModbusLib.Transports;

namespace ModbusLib.Tests.Transports;

public class ChannelTransportTests : IDisposable {
    private readonly ChannelSession _session;
    private readonly ChannelTransport _transport;
    private bool _disposed;

    public ChannelTransportTests() {
        _session = new ChannelSession();
        _transport = new ChannelTransport(_session);
    }

    public void Dispose() {
        if (_disposed) return;
        _transport.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithValidSession_SetsProperties() {
        var session = new ChannelSession();
        var transport = new ChannelTransport(session);

        Assert.Equal(-1, transport.Timeout);
        Assert.True(transport.IsConnected);

        transport.Dispose();
    }

    [Fact]
    public void IsConnected_WithValidSession_ReturnsTrue() {
        Assert.True(_transport.IsConnected);
    }

    [Fact]
    public async Task ConnectAsync_ReturnsTrue() {
        var result = await _transport.ConnectAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task DisconnectAsync_CompletesSuccessfully() {
        await _transport.DisconnectAsync();
        Assert.True(_transport.IsConnected);
    }

    [Fact]
    public void IsConnected_AfterDispose_ReturnsFalse() {
        _transport.Dispose();
        Assert.False(_transport.IsConnected);
    }

    [Fact]
    public async Task SendReceiveAsync_AfterDispose_ThrowsObjectDisposedException() {
        _transport.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            _transport.SendReceiveAsync(new byte[] { 0x01, 0x02 }));
    }

    [Fact]
    public async Task SendReceiveAsync_WithValidRequestAndResponse_ReturnsResponse() {
        var request = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var response = new byte[] { 0x01, 0x03, 0x14, 0x00, 0x0A };
        var responseTask = _transport.SendReceiveAsync(request);

        await _session.ServerToClient.Writer.WriteAsync(response);

        var result = await responseTask;
        Assert.NotNull(result);
        Assert.Equal(response, result);
    }

    [Fact]
    public async Task SendReceiveAsync_SemaphorePreventsConcurrentCalls() {
        var request = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var response = new byte[] { 0x01, 0x03, 0x02 };

        var firstTask = _transport.SendReceiveAsync(request);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await _transport.SendReceiveAsync(request, cts.Token);
        });

        await _session.ServerToClient.Writer.WriteAsync(response);

        await firstTask;
    }

    [Fact]
    public void Timeout_SetValue_ReflectsCorrectly() {
        _transport.Timeout = 3000;
        Assert.Equal(3000, _transport.Timeout);

        _transport.Timeout = -1;
        Assert.Equal(-1, _transport.Timeout);
    }

    [Fact]
    public async Task SendReceiveAsync_WithNegativeTimeout_IgnoresTimeout() {
        var session = new ChannelSession();
        var transport = new ChannelTransport(session);

        try {
            var request = new byte[] { 0x01 };
            var response = new byte[] { 0x01, 0x02 };

            var responseTask = transport.SendReceiveAsync(request);

            await session.ServerToClient.Writer.WriteAsync(response);

            var result = await responseTask;
            Assert.Equal(response, result);
        } finally {
            transport.Dispose();
        }
    }

    [Fact]
    public async Task DisposeAsync_CompletesSuccessfully() {
        await _transport.DisposeAsync();
        Assert.False(_transport.IsConnected);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes() {
        _transport.Dispose();
        _transport.Dispose();
        Assert.False(_transport.IsConnected);
    }

    [Fact]
    public async Task SendReceiveAsync_SendsDataToClientToServerChannel() {
        var request = new byte[] { 0x01, 0x02, 0x03 };
        var response = new byte[] { 0x04, 0x05, 0x06 };

        var sendTask = _transport.SendReceiveAsync(request);

        var received = await _session.ClientToServer.Reader.ReadAsync();
        Assert.Equal(request, received);

        await _session.ServerToClient.Writer.WriteAsync(response);

        var result = await sendTask;
        Assert.Equal(response, result);
    }
}
