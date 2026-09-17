using ModbusLib.Clients;
using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Tests.Mocks;
using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Tests.Clients;

/// <summary>
/// TCP 客户端断线自愈行为验证（对应 issue「在 ExecuteRequestAsync 中增加自动重连机制」）。
///
/// 语义约定：
/// <list type="bullet">
/// <item><c>Retries = 0</c>（默认）：任何失败立即上抛，不重连、不重发（与历史行为一致）。</item>
/// <item><c>Retries &gt; 0</c>：「未连接」按可重试的连接故障处理，但仅当调用方表达过连接意图
/// （调用过 <c>Connect</c> / <c>ConnectAsync</c>）时才自动重建会话。</item>
/// <item>显式 <c>DisconnectAsync</c>：清除连接意图，之后不再自动重建。</item>
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

        // 对端重启（设备上电）后，默认配置下仍不自愈：既不发请求，也不重连
        server.Start();
        var beforeRequests = server.RequestsSeen;
        var beforeConnections = server.ConnectionsAccepted;
        await Assert.ThrowsAsync<ModbusConnectionException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.Equal(beforeRequests, server.RequestsSeen);
        Assert.Equal(beforeConnections, server.ConnectionsAccepted);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RetriesGreaterThan0_AfterServerRecovers_NextRequestSelfHeals(int retries) {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        using var client = CreateClient(server.Port, retries: retries);

        Assert.True(await client.ConnectAsync(ct));
        var beforeBreak = await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        // 网线松动 / 设备重启：对端断开
        server.Stop();
        await AssertDisconnectedAsync(client, ct);
        AssertConnectionFailure(await Assert.ThrowsAnyAsync<Exception>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct)));

        // 对端恢复：下一轮请求应自动重建会话（且只重连、不重发）并成功
        server.Start();
        var beforeRequests = server.RequestsSeen;
        var beforeConnections = server.ConnectionsAccepted;
        var afterRecover = await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        Assert.Equal(beforeBreak[0], afterRecover[0]);
        Assert.True(client.IsConnected, "自愈后客户端应处于已连接状态");
        Assert.Equal(1, server.ConnectionsAccepted - beforeConnections);   // 恰好重连一次
        Assert.Equal(1, server.RequestsSeen - beforeRequests);             // 恰好发出一个请求（重连不构成"重发"）
    }

    [Fact]
    public async Task NeverConnected_DoesNotConnectImplicitly() {
        var ct = TestContext.Current.CancellationToken;
        var server = StartServer();
        using var client = CreateClient(server.Port, retries: 2);

        // 调用方从未表达连接意图：保持"未连接即抛错"，不把编程错误隐藏成"请求偶尔能用"
        var ex = await Assert.ThrowsAsync<ModbusConnectionException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));
        Assert.Equal("客户端未连接", ex.Message);
        Assert.False(client.IsConnected);
        Assert.Equal(0, server.ConnectionsAccepted);
        Assert.Equal(0, server.RequestsSeen);
    }

    [Fact]
    public async Task FirstConnectFailed_RecoversAfterServerComesUp() {
        var ct = TestContext.Current.CancellationToken;
        var port = GetFreePort();
        using var client = CreateClient(port, retries: 2);

        // 设备尚未上电：首连失败，但"连接意图"已表达
        AssertConnectionFailure(await Assert.ThrowsAnyAsync<Exception>(() => client.ConnectAsync(ct)));

        // 设备上电后无需调用方干预，请求路径应自行建连
        var server = StartServer(port);
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
        Assert.Equal(2, server.ConnectionsAccepted);   // 首连 + 超时后重连
        Assert.Equal(2, server.RequestsSeen);          // 第 1 个被吞 → 超时断开重连 → 第 2 个成功
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
