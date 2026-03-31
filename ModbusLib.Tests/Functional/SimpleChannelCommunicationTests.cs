using ModbusLib.Models;
using ModbusLib.Transports;
using Xunit.Abstractions;

namespace ModbusLib.Tests.Functional;

public class SimpleChannelCommunicationTests {
    private readonly ITestOutputHelper _output;

    public SimpleChannelCommunicationTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public async Task SimpleChannel_WriteAndRead_Succeeds() {
        var session = new ChannelSession();

        var writeTask = Task.Run(async () => {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            await session.ServerToClient.Writer.WriteAsync(data);
        });

        var readTask = Task.Run(async () => {
            var result = await session.ClientToServer.Reader.ReadAsync();
            return result;
        });

        await writeTask;

        var data = await session.ServerToClient.Reader.ReadAsync();
        Assert.Equal(5, data.Length);
        Assert.Equal(1, data[0]);
        Assert.Equal(5, data[4]);
    }

    [Fact]
    public async Task BidirectionalChannel_ClientToServerToClient_Succeeds() {
        var session = new ChannelSession();

        var serverTask = Task.Run(async () => {
            _output.WriteLine("Server: Waiting for data...");
            var request = await session.ClientToServer.Reader.ReadAsync();
            _output.WriteLine($"Server: Received {request.Length} bytes");

            var response = new byte[] { (byte)(request[0] + 10), request[1], request[2] };
            await session.ServerToClient.Writer.WriteAsync(response);
        });

        var clientTask = Task.Run(async () => {
            var request = new byte[] { 100, 101, 102 };
            await session.ClientToServer.Writer.WriteAsync(request);

            var response = await session.ServerToClient.Reader.ReadAsync();
            return response;
        });

        var response = await clientTask;
        await serverTask;

        Assert.Equal(3, response.Length);
        Assert.Equal(110, response[0]);
    }

    [Fact]
    public async Task ChannelSession_MultipleMessagesInSequence_Succeeds() {
        var session = new ChannelSession();

        var messages = new List<byte[]> {
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 },
            new byte[] { 7, 8, 9 }
        };

        foreach (var msg in messages) {
            await session.ClientToServer.Writer.WriteAsync(msg);
        }

        var receivedMessages = new List<byte[]>();
        for (int i = 0; i < messages.Count; i++) {
            var data = await session.ClientToServer.Reader.ReadAsync();
            receivedMessages.Add(data.ToArray());
        }

        Assert.Equal(3, receivedMessages.Count);
        Assert.Equal(messages[0], receivedMessages[0]);
        Assert.Equal(messages[1], receivedMessages[1]);
        Assert.Equal(messages[2], receivedMessages[2]);
    }

    [Fact]
    public async Task ChannelTransport_FullRoundTrip_Succeeds() {
        var session = new ChannelSession();
        using var transport = new ChannelTransport(session);

        var serverTask = Task.Run(async () => {
            var request = await session.ClientToServer.Reader.ReadAsync();
            var response = new byte[request.Length];
            for (int i = 0; i < request.Length; i++) {
                response[i] = (byte)(request[i] + 1);
            }
            await session.ServerToClient.Writer.WriteAsync(response);
        });

        var requestData = new byte[] { 10, 20, 30, 40 };
        var responseData = await transport.SendReceiveAsync(requestData);

        await serverTask;

        Assert.Equal(4, responseData.Length);
        Assert.Equal(11, responseData[0]);
        Assert.Equal(21, responseData[1]);
        Assert.Equal(31, responseData[2]);
        Assert.Equal(41, responseData[3]);
    }

    [Fact]
    public async Task ChannelSession_ConcurrentReadWrite_Succeeds() {
        var session = new ChannelSession();
        var iterations = 10;
        var completedWrites = 0;
        var completedReads = 0;

        var writeTask = Task.Run(async () => {
            for (int i = 0; i < iterations; i++) {
                await session.ServerToClient.Writer.WriteAsync([(byte)i]);
                completedWrites++;
            }
        });

        var readTask = Task.Run(async () => {
            for (int i = 0; i < iterations; i++) {
                var data = await session.ServerToClient.Reader.ReadAsync();
                Assert.Single(data);
                completedReads++;
            }
        });

        await Task.WhenAll(writeTask, readTask);

        Assert.Equal(iterations, completedWrites);
        Assert.Equal(iterations, completedReads);
    }
}
