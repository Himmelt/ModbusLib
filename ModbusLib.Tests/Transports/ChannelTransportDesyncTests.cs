using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Transports;
using ModbusLib.Utils;

namespace ModbusLib.Tests.Transports;

/// <summary>
/// Channel 传输的“超时后请求/响应失步”回归测试。
/// 背景：RTU 无事务标识，一旦超时后不清空会话通道，残留响应会被下一轮当成自己的响应读取，
/// 造成永久性失步（长度不匹配 / 静默读到错位数据），且库自身无法自愈。
/// </summary>
public class ChannelTransportDesyncTests {

    private const byte UnitId = 0x01;
    private const byte ReadHoldingRegisters = 0x03;

    /// <summary>构造 RTU 读保持寄存器响应帧（含 CRC）。</summary>
    private static byte[] BuildRtuResponse(byte unitId, ushort quantity, ushort value) {
        var byteCount = quantity * 2;
        var frame = new byte[3 + byteCount + 2];
        frame[0] = unitId;
        frame[1] = ReadHoldingRegisters;
        frame[2] = (byte)byteCount;

        for (var i = 0; i < quantity; i++) {
            frame[3 + i * 2] = (byte)(value >> 8);
            frame[3 + i * 2 + 1] = (byte)(value & 0xFF);
        }

        var crc = Crc16Utils.CalculateCrc16(frame, 0, frame.Length - 2);
        frame[^2] = (byte)(crc & 0xFF);
        frame[^1] = (byte)(crc >> 8);
        return frame;
    }

    private static ushort ReadQuantity(byte[] request) => (ushort)((request[4] << 8) | request[5]);

    /// <summary>
    /// 假对端：按顺序应答请求，值为 0x1000 + 请求序号，便于识别“读到的是哪一轮的响应”。
    /// <paramref name="firstRequestDelayMs"/> 用于模拟首轮迟到的慢链路；
    /// <paramref name="preludeOnFirstRequest"/> 会在第 1 个请求的“正式响应之前”先写入一段字节，
    /// 用来模拟“上一轮残留帧在本次等待期间到达”的情形（这段字节位于发请求排空之后，只能靠帧校验拦住）。
    /// </summary>
    private static (Task Task, CancellationTokenSource Cts) StartDevice(
        ChannelSession session, int firstRequestDelayMs = 0, byte[]? preludeOnFirstRequest = null) {

        var cts = new CancellationTokenSource();
        var seq = 0;

        var task = Task.Run(async () => {
            while (!cts.IsCancellationRequested) {
                var request = await session.ClientToServer.Reader.ReadAsync(cts.Token);
                var n = Interlocked.Increment(ref seq);

                if (n == 1 && firstRequestDelayMs > 0) {
                    await Task.Delay(firstRequestDelayMs, cts.Token);
                }

                if (n == 1 && preludeOnFirstRequest != null) {
                    await session.ServerToClient.Writer.WriteAsync(preludeOnFirstRequest, cts.Token);
                }

                await session.ServerToClient.Writer.WriteAsync(
                    BuildRtuResponse(request[0], ReadQuantity(request), (ushort)(0x1000 + n)), cts.Token);
            }
        });

        return (task, cts);
    }

    /// <summary>超时后的迟到响应必须在下一轮开始前被丢弃：下一轮读到的必须是自己的响应。</summary>
    [Fact]
    public async Task SendReceiveAsync_AfterTimeout_NextRoundReadsItsOwnResponse() {
        const int timeoutMs = 200;
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session, ProtocolType.Rtu) { Timeout = timeoutMs, Retries = 0 };

        var (device, cts) = StartDevice(session, firstRequestDelayMs: timeoutMs + 120);
        try {
            await Assert.ThrowsAsync<ModbusTimeoutException>(() =>
                client.ReadHoldingRegistersAsync(UnitId, 0, 2, cancelToken: TestContext.Current.CancellationToken));

            // 等迟到响应确实落进通道（此时它已经是无人认领的历史帧）
            await Task.Delay(timeoutMs + 100, TestContext.Current.CancellationToken);
            Assert.True(session.ServerToClient.Reader.Count > 0, "前置条件：迟到响应应已落入通道");

            var values = await client.ReadHoldingRegistersAsync(
                UnitId, 0, 2, cancelToken: TestContext.Current.CancellationToken);

            // 第 2 个请求的响应（0x1002），而不是第 1 个请求的迟到响应（0x1001）
            Assert.Equal(0x1002, values[0]);
        } finally {
            cts.Cancel();
            try { await device; } catch { /* 收尾 */ }
            cts.Dispose();
        }
    }

    /// <summary>通道里预置的错配残留帧必须先被丢弃，不能当成本轮响应（RTU 下曾导致永久长度不匹配）。</summary>
    [Fact]
    public async Task SendReceiveAsync_WithStaleFramesInChannel_DiscardsThemBeforeReading() {
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session, ProtocolType.Rtu) { Timeout = 2000 };

        // 3 帧同形状残留 + 1 帧不同形状（qty 不同）残留
        var testToken = TestContext.Current.CancellationToken;
        await session.ServerToClient.Writer.WriteAsync(BuildRtuResponse(UnitId, 2, 0xAAAA), testToken);
        await session.ServerToClient.Writer.WriteAsync(BuildRtuResponse(UnitId, 2, 0xBBBB), testToken);
        await session.ServerToClient.Writer.WriteAsync(BuildRtuResponse(UnitId, 1, 0xCCCC), testToken);

        var (device, cts) = StartDevice(session);
        try {
            for (var round = 1; round <= 4; round++) {
                var values = await client.ReadHoldingRegistersAsync(
                    UnitId, 0, 2, cancelToken: TestContext.Current.CancellationToken);

                Assert.Equal(0x1000 + round, values[0]);
            }
        } finally {
            cts.Cancel();
            try { await device; } catch { /* 收尾 */ }
            cts.Dispose();
        }
    }

    /// <summary>
    /// 抖动模型：首轮响应晚于 Timeout，之后对端完全正常。失步必须只影响当轮，
    /// 后续轮次（含形状随轮次变化的轮询）不得持续失败。
    /// </summary>
    [Fact]
    public async Task SendReceiveAsync_AfterTransientTimeout_RealignsWithoutUpperLayerReconnect() {
        const int timeoutMs = 200;
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session, ProtocolType.Rtu) { Timeout = timeoutMs, Retries = 1 };

        var (device, cts) = StartDevice(session, firstRequestDelayMs: timeoutMs + 120);
        try {
            var failures = new List<Exception>();

            for (var round = 1; round <= 8; round++) {
                // 形状随轮次变化：qty 1/2 交替（真实轮询里各数据块的响应长度不同）
                var quantity = (ushort)(round % 2 == 0 ? 2 : 1);
                try {
                    var values = await client.ReadHoldingRegistersAsync(
                        UnitId, 0, quantity, cancelToken: TestContext.Current.CancellationToken);

                    Assert.Equal(quantity, values.Length);
                } catch (Exception ex) when (
                    ex is ModbusTimeoutException or ModbusCommunicationException or ModbusConnectionException) {
                    // 只容忍通信层失败（第 1~2 轮允许因首轮迟到超时）；断言失败、其它异常一律直接暴露
                    if (round >= 3) failures.Add(ex);
                }
            }

            Assert.Empty(failures);
        } finally {
            cts.Cancel();
            try { await device; } catch { /* 收尾 */ }
            cts.Dispose();
        }
    }

    /// <summary>DisconnectAsync 在 Channel 传输上具备复位语义：清空滞留帧，使重试真正可自愈。</summary>
    [Fact]
    public async Task DisconnectAsync_DiscardsPendingFrames() {
        var session = new ChannelSession();
        using var transport = new ChannelTransport(session, ProtocolType.Rtu);

        var testToken = TestContext.Current.CancellationToken;
        await session.ServerToClient.Writer.WriteAsync(BuildRtuResponse(UnitId, 2, 0xAAAA), testToken);
        await session.ServerToClient.Writer.WriteAsync(BuildRtuResponse(UnitId, 2, 0xBBBB), testToken);
        Assert.Equal(2, session.ServerToClient.Reader.Count);

        await transport.DisconnectAsync(testToken);

        Assert.Equal(0, session.ServerToClient.Reader.Count);
        Assert.True(transport.IsConnected);
    }

    /// <summary>
    /// 半帧片段（上一轮超时残留的帧尾）在本次等待期间到达时，必须靠帧起始校验识别并丢弃，
    /// 绝不能拿它去推算帧长、更不能当成本次响应。
    /// </summary>
    [Fact]
    public async Task SendReceiveAsync_WithOrphanFrameFragment_DiscardsFragmentAndReadsResponse() {
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session, ProtocolType.Rtu) { Timeout = 1000 };

        // 完整响应去掉前 3 字节后剩下的“尾巴”：首字节不再是设备地址
        var orphanFragment = BuildRtuResponse(UnitId, 2, 0x9999)[3..];

        var (device, cts) = StartDevice(session, preludeOnFirstRequest: orphanFragment);
        try {
            var values = await client.ReadHoldingRegistersAsync(
                UnitId, 0, 2, cancelToken: TestContext.Current.CancellationToken);

            Assert.Equal(0x1001, values[0]);
        } finally {
            cts.Cancel();
            try { await device; } catch { /* 收尾 */ }
            cts.Dispose();
        }
    }

    /// <summary>
    /// 形状与本次请求不符的残留帧（qty 不同 → 帧长不同）在等待期间到达时，
    /// 必须靠帧长校验丢弃并继续等自己的响应。
    /// </summary>
    [Fact]
    public async Task SendReceiveAsync_WithStaleFrameOfWrongShape_DiscardsItAndWaitsForOwnResponse() {
        var session = new ChannelSession();
        using var client = new ModbusChannelClient(session, ProtocolType.Rtu) { Timeout = 1000 };

        // 本次请求 qty=2（期望 9 字节响应），残留帧是 qty=1（7 字节）
        var staleFrame = BuildRtuResponse(UnitId, 1, 0x8888);

        var (device, cts) = StartDevice(session, preludeOnFirstRequest: staleFrame);
        try {
            var values = await client.ReadHoldingRegistersAsync(
                UnitId, 0, 2, cancelToken: TestContext.Current.CancellationToken);

            Assert.Equal(0x1001, values[0]);
        } finally {
            cts.Cancel();
            try { await device; } catch { /* 收尾 */ }
            cts.Dispose();
        }
    }

    /// <summary>有界会话：写满后丢弃最旧的滞留帧（兜底防内存无上限增长）。</summary>
    [Fact]
    public async Task ChannelSession_WithCapacity_BoundsServerToClientQueue() {
        var session = new ChannelSession(2);

        Assert.True(session.ServerToClient.Writer.TryWrite([0x01]));
        Assert.True(session.ServerToClient.Writer.TryWrite([0x02]));
        Assert.True(session.ServerToClient.Writer.TryWrite([0x03]));

        Assert.Equal(2, session.ServerToClient.Reader.Count);
        Assert.Equal(new byte[] { 0x02 }, await session.ServerToClient.Reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(new byte[] { 0x03 }, await session.ServerToClient.Reader.ReadAsync(TestContext.Current.CancellationToken));

        // 请求方向保持无界
        Assert.True(session.ClientToServer.Writer.TryWrite([0x01]));
        Assert.True(session.ClientToServer.Writer.TryWrite([0x02]));
        Assert.True(session.ClientToServer.Writer.TryWrite([0x03]));
        Assert.Equal(3, session.ClientToServer.Reader.Count);
    }

    /// <summary>容量参数非法时立即失败。</summary>
    [Fact]
    public void ChannelSession_WithInvalidCapacity_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChannelSession(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChannelSession(-1));
    }
}
