using ModbusLib.Clients;
using ModbusLib.Interfaces;
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

        var isConnected = await _client.ConnectAsync();
        Assert.True(isConnected);
        Assert.True(_client.IsConnected);
    }

    [Fact]
    public async Task ModbusChannelClient_WriteSingleCoil_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync();

        await _client.WriteSingleCoilAsync(1, 0, true);

        Assert.True(_server.GetCoil(0));
    }

    [Fact]
    public async Task ModbusChannelClient_ReadCoils_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        _server.SetCoil(0, true);
        _server.SetCoil(1, false);
        _server.SetCoil(2, true);

        await _client.ConnectAsync();

        var coils = await _client.ReadCoilsAsync(1, 0, 3);

        Assert.True(coils[0]);
        Assert.False(coils[1]);
        Assert.True(coils[2]);
    }

    [Fact]
    public async Task ModbusChannelClient_WriteSingleRegister_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync();

        await _client.WriteSingleRegisterAsync(1, 100, 12345);

        Assert.Equal(12345, _server.GetHoldingRegister(100));
    }

    [Fact]
    public async Task ModbusChannelClient_ReadHoldingRegisters_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        _server.SetHoldingRegister(0, 11111);
        _server.SetHoldingRegister(1, 22222);

        await _client.ConnectAsync();

        var registers = await _client.ReadHoldingRegistersAsync(1, 0, 2);

        Assert.Equal(11111, registers[0]);
        Assert.Equal(22222, registers[1]);
    }

    [Fact]
    public async Task ModbusChannelClient_MultipleOperations_Test() {
        _server = CreateServer();
        _client = CreateClient(_server);

        await _client.ConnectAsync();

        await _client.WriteSingleCoilAsync(1, 0, true);
        Assert.True(_server.GetCoil(0));

        await _client.WriteSingleRegisterAsync(1, 100, 9999);
        Assert.Equal(9999, _server.GetHoldingRegister(100));

        var coils = await _client.ReadCoilsAsync(1, 0, 1);
        Assert.True(coils[0]);

        var registers = await _client.ReadHoldingRegistersAsync(1, 100, 1);
        Assert.Equal(9999, registers[0]);
    }

    [Fact]
    public async Task ModbusChannelClient_CreateTcpClient_Works() {
        _server = CreateServer();
        _client = new ModbusChannelClient(_server.Session);

        await _client.ConnectAsync();
        Assert.True(_client.IsConnected);

        await _client.WriteSingleRegisterAsync(1, 50, 5555);
        Assert.Equal(5555, _server.GetHoldingRegister(50));
    }

    [Fact]
    public async Task ModbusChannelClient_CreateRtuClient_Works() {
        _server = CreateServer();
        _client = new ModbusChannelClient(_server.Session);

        await _client.ConnectAsync();
        Assert.True(_client.IsConnected);
    }
}