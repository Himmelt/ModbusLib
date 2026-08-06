using ModbusLib.Models;
using ModbusLib.Transports;

namespace ModbusLib.Tests.Transports;

public class PipeTransportTests : IDisposable {
    private readonly PipeSession _session;
    private readonly PipeTransport _transport;
    private bool _disposed;

    public PipeTransportTests() {
        _session = new PipeSession();
        _transport = new PipeTransport(_session);
    }

    public void Dispose() {
        if (_disposed) return;
        _transport.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithValidPipes_SetsProperties() {
        var _session = new PipeSession();

        var transport = new PipeTransport(_session);

        Assert.Equal(-1, transport.Timeout);
        Assert.True(transport.IsConnected);

        transport.Dispose();
    }

    [Fact]
    public void IsConnected_WithValidPipes_ReturnsTrue() {
        Assert.True(_transport.IsConnected);
    }

    [Fact]
    public async Task ConnectAsync_ReturnsTrue() {
        var result = await _transport.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(result);
    }

    [Fact]
    public async Task DisconnectAsync_CompletesSuccessfully() {
        await _transport.DisconnectAsync(TestContext.Current.CancellationToken);
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
            _transport.SendReceiveAsync([0x01, 0x02], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendReceiveAsync_WithValidRequestAndResponse_ReturnsResponse() {
        var request = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var response = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x06, 0x00, 0x64, 0x30, 0x39 };
        var responseTask = _transport.SendReceiveAsync(request, TestContext.Current.CancellationToken);

        await _session.ServerToClient.Writer.WriteAsync(response, TestContext.Current.CancellationToken);
        _session.ServerToClient.Writer.Complete();

        var result = await responseTask;
        Assert.NotNull(result);
        Assert.Equal(response.Length, result.Length);
    }

    [Fact]
    public async Task SendReceiveAsync_SplitResponse_AssemblesCompleteFrame() {
        var session = new PipeSession();
        using var transport = new PipeTransport(session);

        var response = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x06, 0x00, 0x64, 0x30, 0x39 };
        var responseTask = transport.SendReceiveAsync([0x01], TestContext.Current.CancellationToken);

        await session.ServerToClient.Writer.WriteAsync(response.AsMemory(0, 6), TestContext.Current.CancellationToken);
        await Task.Delay(10, TestContext.Current.CancellationToken);
        await session.ServerToClient.Writer.WriteAsync(response.AsMemory(6), TestContext.Current.CancellationToken);
        session.ServerToClient.Writer.Complete();

        var result = await responseTask;
        Assert.Equal(response, result);
    }

    [Fact]
    public async Task SendReceiveAsync_SemaphorePreventsConcurrentCalls() {
        var request = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        var response = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x06, 0x00, 0x64, 0x30, 0x39 };

        var firstTask = _transport.SendReceiveAsync(request, TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => {
            await _transport.SendReceiveAsync(request, cts.Token);
        });

        await _session.ServerToClient.Writer.WriteAsync(response, TestContext.Current.CancellationToken);
        _session.ServerToClient.Writer.Complete();

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
        var session = new PipeSession();
        var transport = new PipeTransport(session);

        try {
            var request = new byte[] { 0x01 };
            var response = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x06, 0x00, 0x64, 0x30, 0x39 };

            var responseTask = transport.SendReceiveAsync(request, TestContext.Current.CancellationToken);

            await session.ServerToClient.Writer.WriteAsync(response, TestContext.Current.CancellationToken);
            session.ServerToClient.Writer.Complete();

            await responseTask;
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
}
