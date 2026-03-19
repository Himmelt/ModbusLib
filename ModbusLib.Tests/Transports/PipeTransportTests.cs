using System.IO.Pipelines;
using ModbusLib.Exceptions;
using ModbusLib.Protocols;
using ModbusLib.Transports;

namespace ModbusLib.Tests.Transports;

public class PipeTransportTests : IDisposable {
    private readonly Pipe _pipeIn;
    private readonly Pipe _pipeOut;
    private readonly PipeTransport _transport;
    private bool _disposed;

    public PipeTransportTests() {
        _pipeIn = new Pipe();
        _pipeOut = new Pipe();
        _transport = new PipeTransport(_pipeIn, _pipeOut, 5000);
    }

    public void Dispose() {
        if (_disposed) return;
        _transport.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithValidPipes_SetsProperties() {
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        var transport = new PipeTransport(pipeIn, pipeOut, 3000);

        Assert.Equal(3000, transport.Timeout);
        Assert.True(transport.IsConnected);

        transport.Dispose();
    }

    [Fact]
    public void Constructor_WithNullPipeIn_ThrowsArgumentNullException() {
        var pipeOut = new Pipe();
        Assert.Throws<ArgumentNullException>(() => new PipeTransport(null!, pipeOut));
    }

    [Fact]
    public void Constructor_WithNullPipeOut_ThrowsArgumentNullException() {
        var pipeIn = new Pipe();
        Assert.Throws<ArgumentNullException>(() => new PipeTransport(pipeIn, null!));
    }

    [Fact]
    public void IsConnected_WithValidPipes_ReturnsTrue() {
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

        await _pipeIn.Writer.WriteAsync(response);
        _pipeIn.Writer.Complete();

        var result = await responseTask;
        Assert.NotNull(result);
        Assert.Equal(response.Length, result.Length);
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

        await _pipeIn.Writer.WriteAsync(response);
        _pipeIn.Writer.Complete();

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
        var pipeIn = new Pipe();
        var pipeOut = new Pipe();
        var transport = new PipeTransport(pipeIn, pipeOut, -1);

        try {
            var request = new byte[] { 0x01 };
            var response = new byte[] { 0x01, 0x02 };

            var responseTask = transport.SendReceiveAsync(request);

            await pipeIn.Writer.WriteAsync(response);
            pipeIn.Writer.Complete();

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