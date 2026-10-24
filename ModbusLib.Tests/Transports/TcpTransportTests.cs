using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Transports;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Tests.Transports;

public class TcpTransportTests : IDisposable {
    private TcpListener? _listener;

    public void Dispose() {
        GC.SuppressFinalize(this);
        _listener?.Stop();
    }

    private async Task<TcpListener> StartListenerAsync() {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        _listener = listener;
        return listener;
    }

    private static byte[] WriteSingleRegisterResponse(ushort transactionId = 1) {
        return
        [
            (byte)(transactionId >> 8),
            (byte)(transactionId & 0xFF),
            0x00, 0x00,
            0x00, 0x06,
            0x01, 0x06,
            0x00, 0x64,
            0x30, 0x39
        ];
    }

    [Fact]
    public async Task SendReceiveAsync_ServerSplitsResponse_ReturnsCompleteFrame() {
        var ct = TestContext.Current.CancellationToken;
        var listener = await StartListenerAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var transport = new TcpTransport(new NetworkConfig { RemoteHost = "127.0.0.1", RemotePort = port });
        await transport.ConnectAsync(ct);

        var response = WriteSingleRegisterResponse();
        var requestBytes = new byte[12];
        var serverTask = Task.Run(async () => {
            using var client = await listener.AcceptTcpClientAsync(ct);
            using var stream = client.GetStream();
            await stream.ReadExactlyAsync(requestBytes, ct);
            await stream.WriteAsync(response.AsMemory(0, 6), ct);
            await Task.Delay(30, ct);
            await stream.WriteAsync(response.AsMemory(6), ct);
        }, ct);

        var result = await transport.SendReceiveAsync(requestBytes, ct);
        await serverTask;

        Assert.Equal(response, result);
    }

    [Fact]
    public async Task SendReceiveAsync_Timeout_KeepsConnectionAndLinkStateUp() {
        var ct = TestContext.Current.CancellationToken;
        var listener = await StartListenerAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var transport = new TcpTransport(new NetworkConfig {
            RemoteHost = "127.0.0.1",
            RemotePort = port,
            ReceiveTimeout = 100
        });
        await transport.ConnectAsync(ct);

        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = Task.Run(async () => {
            using var client = await listener.AcceptTcpClientAsync(ct);
            using var stream = client.GetStream();
            await stream.ReadExactlyAsync(new byte[12], ct);
            await release.Task; // 保持连接打开，不响应请求
        }, ct);

        // 从站没回话是"这一轮没数据"，不是"链路坏了"：连接必须保留
        await Assert.ThrowsAsync<ModbusTimeoutException>(() =>
            transport.SendReceiveAsync(new byte[12], ct));

        Assert.True(transport.IsConnected);
        Assert.Equal(LinkState.Up, transport.LinkState);

        release.SetResult();
        await serverTask;
    }

    [Fact]
    public async Task SendReceiveAsync_PeerClosesConnection_MarksLinkBroken() {
        var ct = TestContext.Current.CancellationToken;
        var listener = await StartListenerAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var transport = new TcpTransport(new NetworkConfig {
            RemoteHost = "127.0.0.1",
            RemotePort = port,
            ReceiveTimeout = 5000
        });
        await transport.ConnectAsync(ct);

        var serverTask = Task.Run(async () => {
            using var client = await listener.AcceptTcpClientAsync(ct);
            using var stream = client.GetStream();
            await stream.ReadExactlyAsync(new byte[12], ct);
            // 对端直接关闭：这是明确的链路级证据
        }, ct);

        var ex = await Assert.ThrowsAsync<ModbusCommunicationException>(() =>
            transport.SendReceiveAsync(new byte[12], ct));
        Assert.True(ex.IsLinkBroken);
        Assert.Equal(LinkState.Down, transport.LinkState);
        Assert.False(transport.IsConnected);

        await serverTask;
    }

    [Fact]
    public async Task SendReceiveAsync_UserCancellation_ThrowsOperationCanceledException() {
        var ct = TestContext.Current.CancellationToken;
        var listener = await StartListenerAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var transport = new TcpTransport(new NetworkConfig {
            RemoteHost = "127.0.0.1",
            RemotePort = port,
            ReceiveTimeout = 5000
        });
        await transport.ConnectAsync(ct);

        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = Task.Run(async () => {
            using var client = await listener.AcceptTcpClientAsync(ct);
            using var stream = client.GetStream();
            await stream.ReadExactlyAsync(new byte[12], ct);
            await release.Task;
        }, ct);

        using var cts = new CancellationTokenSource(100);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            transport.SendReceiveAsync(new byte[12], cts.Token));

        release.SetResult();
        await serverTask;
    }
}
