using ModbusLib.Clients;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Tests.Mocks;

namespace ModbusLib.Tests.Functional;

public class ModbusChannelClientIntegrationTests : IDisposable {
    private MockChannelModbusServer? _server;
    private IModbusClient? _client;
    private bool _disposed;

    public ModbusChannelClientIntegrationTests() {
    }

    private MockChannelModbusServer CreateServer() {
        var server = new MockChannelModbusServer();
        server.Start();
        return server;
    }

    private IModbusClient CreateClient(MockChannelModbusServer server) {
        var session = server.Session;
        return new ModbusChannelClient(session);
    }

    public void Dispose() {
        if (_disposed) return;
        _client?.Dispose();
        _server?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ModbusChannelClient_Connect_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        var isConnected = await _client.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(isConnected);
        Assert.True(_client.IsConnected);
    }

    [Fact]
    public async Task ModbusChannelClient_WriteSingleCoil_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);

        await _client.WriteSingleCoilAsync(1, 0, true, TestContext.Current.CancellationToken);

        Assert.True(_server.GetCoil(0));
    }

    [Fact]
    public async Task ModbusChannelClient_ReadCoils_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        _server.SetCoil(0, true);
        _server.SetCoil(1, false);
        _server.SetCoil(2, true);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);

        var coils = await _client.ReadCoilsAsync(1, 0, 3, TestContext.Current.CancellationToken);

        Assert.True(coils[0]);
        Assert.False(coils[1]);
        Assert.True(coils[2]);
    }

    [Fact]
    public async Task ModbusChannelClient_WriteSingleRegister_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);

        await _client.WriteSingleRegisterAsync(1, 100, 12345, cancelToken: TestContext.Current.CancellationToken);

        Assert.Equal(12345, _server.GetHoldingRegister(100));
    }

    [Fact]
    public async Task ModbusChannelClient_ReadHoldingRegisters_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        _server.SetHoldingRegister(0, 11111);
        _server.SetHoldingRegister(1, 22222);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);

        var registers = await _client.ReadHoldingRegistersAsync(1, 0, 2, cancelToken: TestContext.Current.CancellationToken);

        Assert.Equal(11111, registers[0]);
        Assert.Equal(22222, registers[1]);
    }

    [Fact]
    public async Task ModbusChannelClient_MultipleOperations_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);

        await _client.WriteSingleCoilAsync(1, 0, true, TestContext.Current.CancellationToken);
        Assert.True(_server.GetCoil(0));

        await _client.WriteSingleRegisterAsync(1, 100, 9999, cancelToken: TestContext.Current.CancellationToken);
        Assert.Equal(9999, _server.GetHoldingRegister(100));

        var coils = await _client.ReadCoilsAsync(1, 0, 1, TestContext.Current.CancellationToken);
        Assert.True(coils[0]);

        var registers = await _client.ReadHoldingRegistersAsync(1, 100, 1, cancelToken: TestContext.Current.CancellationToken);
        Assert.Equal(9999, registers[0]);
    }

    [Fact]
    public async Task ModbusChannelClient_ByteGenericReadWrite_RoundTrips() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);

        // 3 个 byte 需要 2 个寄存器（第4字节补0），读回时截断到请求数量
        var values = new byte[] { 0x12, 0x34, 0x56 };
        await _client.WriteMultipleRegistersAsync(1, 0, values, cancelToken: TestContext.Current.CancellationToken);

        var read = await _client.ReadHoldingRegistersAsync<byte>(1, 0, 3, cancelToken: TestContext.Current.CancellationToken);

        Assert.Equal(values, read);
    }

    [Fact]
    public async Task ModbusChannelClient_SingleByteWrite_PadsToRegister() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);

        await _client.WriteMultipleRegistersAsync(1, 0, (byte)0xAB, cancelToken: TestContext.Current.CancellationToken);

        var raw = await _client.ReadHoldingRegistersRawAsync(1, 0, 1, cancelToken: TestContext.Current.CancellationToken);
        Assert.Equal(new byte[] { 0xAB, 0x00 }, raw);
    }

    [Fact]
    public async Task WriteMultipleRegistersRawAsync_WithOddBytes_Throws() {
        _client = new ModbusChannelClient(new ChannelSession());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _client.WriteMultipleRegistersRawAsync(1, 0, new byte[] { 0x01, 0x02, 0x03 }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ModbusChannelClient_CreateTcpClient_Works() {
        _server = CreateServer();
        _client = new ModbusChannelClient(_server.Session);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(_client.IsConnected);

        await _client.WriteSingleRegisterAsync(1, 50, 5555, cancelToken: TestContext.Current.CancellationToken);
        Assert.Equal(5555, _server.GetHoldingRegister(50));
    }

    [Fact]
    public async Task ModbusChannelClient_CreateRtuClient_Works() {
        _server = CreateServer();
        _client = new ModbusChannelClient(_server.Session);

        await _client.ConnectAsync(TestContext.Current.CancellationToken);
        Assert.True(_client.IsConnected);
    }
}
