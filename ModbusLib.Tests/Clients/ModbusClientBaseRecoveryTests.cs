using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Tests.Mocks;

namespace ModbusLib.Tests.Clients;

/// <summary>
/// 会话恢复的「传输无关」验证：用测试替身模拟"通信失败后 <c>IsConnected</c> 仍为 <c>true</c>"的传输
/// （这是 <c>SerialTransport</c> 的真实语义，TCP/UDP 失败时会自断连接因而不适用）。
///
/// 覆盖两个要点：
/// <list type="bullet">
/// <item>此时循环内的重建分支**不会**触发，恢复只能靠重试路径上的强制重连；</item>
/// <item><c>Retries = 0</c> 时不会发生任何重连 / 重发。</item>
/// </list>
/// </summary>
public class ModbusClientBaseRecoveryTests {

    [Fact]
    public async Task TransportStaysConnectedAfterFailure_RetryPathRebuildsLink() {
        var ct = TestContext.Current.CancellationToken;
        var transport = new StaleLinkTransport();
        using var client = new StubClient(transport) { Retries = 1 };

        Assert.True(await client.ConnectAsync(ct));
        transport.LinkIsStale = true;                       // 链路已断，但 IsConnected 仍为 true

        var values = await client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct);

        Assert.Equal(0x1011, (int)values[0]);
        Assert.Equal(2, transport.SendCalls);               // 第 1 次失败 + 重试 1 次
        Assert.Equal(2, transport.ConnectCalls);            // 首连 + 重试前的强制重建
    }

    [Fact]
    public async Task Retries0_StaleLink_DoesNotRebuildOrResend() {
        var ct = TestContext.Current.CancellationToken;
        var transport = new StaleLinkTransport();
        using var client = new StubClient(transport) { Retries = 0 };

        Assert.True(await client.ConnectAsync(ct));
        transport.LinkIsStale = true;

        await Assert.ThrowsAsync<ModbusTimeoutException>(
            () => client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: ct));

        Assert.Equal(1, transport.SendCalls);               // 不重发
        Assert.Equal(1, transport.ConnectCalls);            // 不重连
    }

    /// <summary>只做最小桩实现：本组用例验证的是 <c>ModbusClientBase</c> 的恢复调度，不是组帧。</summary>
    private sealed class StubProtocol : IModbusProtocol {

        public byte[] BuildRequest(ModbusRequest request) => new byte[8];

        public bool ValidateResponse(byte[] response) => true;

        public int CalculateExpectedResponseLength(ModbusRequest request) => 5;

        public ModbusResponse ParseResponse(byte[] response, ModbusRequest request) {
            // Data[0] 是字节数，读取 2 个寄存器 = 4 字节数据
            return new ModbusResponse(1, ModbusFunction.ReadHoldingRegisters, [4, 0x10, 0x11, 0x12, 0x13], response);
        }
    }

    private sealed class StubClient(StaleLinkTransport transport) : ModbusClientBase {

        protected override IModbusProtocol Protocol { get; set; } = new StubProtocol();

        protected override IModbusTransport Transport { get; set; } = transport;
    }
}
