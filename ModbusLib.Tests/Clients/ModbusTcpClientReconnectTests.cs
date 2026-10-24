using ModbusLib.Clients;
using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Tests.Mocks;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Tests.Clients;

/// <summary>
/// 断线处置职责划分验证（库只报告，宿主管重连）。
///
/// 语义约定：
/// <list type="bullet">
/// <item><b>链路级故障</b>（对端 FIN/RST、连接未建立）：上抛 <see cref="ModbusConnectionException"/>
/// / <see cref="ModbusCommunicationException"/>（<c>IsLinkBroken = true</c>），
/// <see cref="ModbusClientBase.LinkState"/> 翻转为 <c>Down</c>。库<b>不</b>自动重连，
/// 重连是连接看护者（宿主持有连接的一方）的职责。</item>
/// <item><b>协议级故障</b>（请求超时、帧错配）：连接保留、<c>LinkState</c> 仍为 <c>Up</c>；
/// 此时 <c>Retries &gt; 0</c> 会在同一条链路上重发，绝不重建连接。</item>
/// <item>用户取消：原样抛 <c>OperationCanceledException</c>，既不算故障（不重试）也不重发。</item>
/// </list>
/// </summary>
public class ModbusTcpClientReconnectTests : IDisposable {

    private FakeModbusTcpServer? _server;

    public void Dispose() {
        _server?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Retries0_AfterDisconnect_DoesNotReconnectOrResend() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        using var client = CreateClient(server.Port, retries: 0);

        Assert.True(await client.ConnectAsync(ct));
        Assert.Equal(0x1011, (int)(await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct))[0]);

        server.Stop();
        await AssertDisconnectedAsync(client, ct);

        var ex = await Assert.ThrowsAsync<ModbusConnectionException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.Equal("客户端未连接", ex.Message);

        // 对端重启（设备上电）后库仍不自愈：不发请求、不重连；重连由连接看护者负责
        server.Start();
        var beforeRequests = server.RequestsSeen;
        var beforeConnections = server.ConnectionsAccepted;
        var ex2 = await Assert.ThrowsAsync<ModbusConnectionException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.True(ex2.IsLinkBroken);
        Assert.Equal(LinkState.Down, client.LinkState);
        Assert.Equal(beforeRequests, server.RequestsSeen);
        Assert.Equal(beforeConnections, server.ConnectionsAccepted);
    }

    [Fact]
    public async Task LinkDown_HostReconnects_ThenRequestSucceeds() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        using var client = CreateClient(server.Port, retries: 2);

        Assert.True(await client.ConnectAsync(ct));
        var beforeBreak = await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        // 网线松动 / 设备重启：对端断开
        server.Stop();
        await AssertDisconnectedAsync(client, ct);
        var broken = await Assert.ThrowsAnyAsync<Exception>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        AssertConnectionFailure(broken);
        Assert.Equal(LinkState.Down, client.LinkState);

        // 库不会自己重连：请求只报错，不发帧
        var beforeRequests = server.RequestsSeen;
        await Assert.ThrowsAnyAsync<Exception>(() => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.Equal(beforeRequests, server.RequestsSeen);

        // 对端恢复后，由连接看护者（宿主）显式重连，之后请求即恢复
        server.Start();
        Assert.True(await client.ConnectAsync(ct));
        var afterRecover = await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        Assert.Equal(beforeBreak[0], afterRecover[0]);
        Assert.True(client.IsConnected);
        Assert.Equal(LinkState.Up, client.LinkState);
    }

    [Fact]
    public async Task NeverConnected_DoesNotConnectImplicitly() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        using var client = CreateClient(server.Port, retries: 2);

        // 从未连接过：保持"未连接即抛错"，不会隐式建连
        var ex = await Assert.ThrowsAsync<ModbusConnectionException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.Equal("客户端未连接", ex.Message);
        Assert.True(ex.IsLinkBroken);
        Assert.False(client.IsConnected);
        Assert.Equal(0, server.ConnectionsAccepted);
        Assert.Equal(0, server.RequestsSeen);
    }

    [Fact]
    public async Task FirstConnectFailed_HostReconnectSucceedsAfterServerComesUp() {
        var ct = TestContext.Current.CancellationToken;
        var port = GetFreePort();
        using var client = CreateClient(port, retries: 2);

        // 设备尚未上电：首连失败，链路标记为 Down
        AssertConnectionFailure(await Assert.ThrowsAnyAsync<Exception>(() => client.ConnectAsync(ct)));
        Assert.Equal(LinkState.Down, client.LinkState);

        // 设备上电后库不会自行建连：请求只会报"未连接"
        var server = StartServer(port);
        await Assert.ThrowsAsync<ModbusConnectionException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.Equal(0, server.ConnectionsAccepted);

        // 由连接看护者（宿主）重连，之后请求恢复
        Assert.True(await client.ConnectAsync(ct));
        var values = await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        Assert.Equal(0x1011, (int)values[0]);
        Assert.Equal(1, server.ConnectionsAccepted);
        Assert.Equal(1, server.RequestsSeen);
        Assert.True(client.IsConnected);
    }

    [Fact]
    public async Task ExplicitDisconnect_IsNotOverriddenByAutoReconnect() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        using var client = CreateClient(server.Port, retries: 2);

        Assert.True(await client.ConnectAsync(ct));
        await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        await client.DisconnectAsync(ct);
        Assert.False(client.IsConnected);

        // 显式停机（或切换设备）后，对端仍在也不能被自动重连顶回来
        var beforeRequests = server.RequestsSeen;
        var beforeConnections = server.ConnectionsAccepted;
        var ex = await Assert.ThrowsAsync<ModbusConnectionException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.Equal("客户端未连接", ex.Message);
        Assert.Equal(beforeRequests, server.RequestsSeen);
        Assert.Equal(beforeConnections, server.ConnectionsAccepted);
    }

    [Fact]
    public async Task TimeoutAfterRequestSent_StillSelfHealsByRetry() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        server.IgnoreFirstRequests = 1;          // 第 1 个请求只收不回，制造客户端超时
        using var client = CreateClient(server.Port, retries: 2);

        Assert.True(await client.ConnectAsync(ct));
        var values = await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        Assert.Equal(0x1011, (int)values[0]);
        Assert.Equal(1, server.ConnectionsAccepted);   // 超时不断连接：全程一条连接
        Assert.Equal(2, server.RequestsSeen);          // 第 1 个被吞 → 同链路重发 → 第 2 个成功
        Assert.True(client.IsConnected);
    }

    [Fact]
    public async Task AlreadyCancelledToken_PropagatesCancellation_WithoutSendingRequest() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        using var client = CreateClient(server.Port, retries: 2);

        Assert.True(await client.ConnectAsync(ct));
        await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var beforeRequests = server.RequestsSeen;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: cts.Token));

        // 用户取消不是"故障"：不重试、不重发、不重建会话
        Assert.Equal(beforeRequests, server.RequestsSeen);
        Assert.True(client.IsConnected, "用户取消不应破坏已建立的连接");
    }

    [Fact]
    public async Task CancelDuringRequest_PropagatesCancellation_InsteadOfRetrying() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        server.IgnoreFirstRequests = int.MaxValue;                  // 对端不回帧，请求会一直等待
        using var client = CreateClient(server.Port, retries: 2, requestTimeout: 10000);
        Assert.True(await client.ConnectAsync(ct));
        await Task.Delay(150, ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(300);                                       // 远早于 10s 的请求超时

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: cts.Token));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 5000, $"用户取消应立即生效，实际耗时 {sw.ElapsedMilliseconds}ms");
        Assert.Equal(1, server.RequestsSeen);                        // 取消不触发重发
        Assert.Equal(1, server.ConnectionsAccepted);
    }

    #region 辅助

    private FakeModbusTcpServer StartServer(int port = 0) {
        _server = new FakeModbusTcpServer(port);
        _server.Start();
        return _server;
    }

    private static ModbusTcpClient CreateClient(int port, int retries, int requestTimeout = 2000) {
        return new ModbusTcpClient(new NetworkConfig {
            RemoteHost = "127.0.0.1",
            RemotePort = port,
            ConnectTimeout = 800,
            ReceiveTimeout = 600,
            SendTimeout = 600
        }) {
            Timeout = requestTimeout,
            Retries = retries
        };
    }

    /// <summary>
    /// 对端不可达时的异常形态取决于平台与网络栈：拒连（RST）→ <c>ModbusConnectionException</c>；
    /// 无任何响应直到 ConnectTimeout → <c>ModbusTimeoutException</c>。两者都属"连接类故障"。
    /// </summary>
    private static void AssertConnectionFailure(Exception ex) {
        Assert.True(ex is ModbusConnectionException or ModbusTimeoutException,
            $"应抛连接类异常，实际 {ex.GetType().Name}: {ex.Message}");
    }

    /// <summary>等待客户端察觉对端已断开（TCP FIN 需要一点时间才能被 Poll 观察到）。</summary>
    private static async Task AssertDisconnectedAsync(ModbusTcpClient client, CancellationToken ct) {
        for (var i = 0; i < 100 && client.IsConnected; i++) {
            await Task.Delay(20, ct);
        }
        Assert.False(client.IsConnected, "对端已关闭连接，但客户端 IsConnected 仍为 true");
    }

    private static int GetFreePort() {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    #endregion
}
