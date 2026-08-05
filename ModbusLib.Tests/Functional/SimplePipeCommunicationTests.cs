using System.Buffers;
using System.IO.Pipelines;

namespace ModbusLib.Tests.Functional;

public class SimplePipeCommunicationTests {
    private readonly ITestOutputHelper _output;

    public SimplePipeCommunicationTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public async Task SimplePipe_WriteAndRead_Succeeds() {
        var pipe = new Pipe();

        var writeTask = Task.Run(async () => {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            await pipe.Writer.WriteAsync(data);
            pipe.Writer.Complete();
        }, TestContext.Current.CancellationToken);

        var readTask = Task.Run(async () => {
            var result = await pipe.Reader.ReadAsync();
            var buffer = result.Buffer;
            var data = new byte[buffer.Length];
            var index = 0;
            foreach (var segment in buffer) {
                segment.Span.CopyTo(data.AsSpan(index));
                index += segment.Length;
            }
            return data;
        });

        var data = await readTask;
        await writeTask;

        Assert.Equal(5, data.Length);
        Assert.Equal(1, data[0]);
        Assert.Equal(5, data[4]);
    }

    [Fact]
    public async Task BidirectionalPipe_ClientToServerToClient_Succeeds() {
        var pipe1 = new Pipe();
        var pipe2 = new Pipe();

        var serverTask = Task.Run(async () => {
            var reader = pipe1.Reader;
            var writer = pipe2.Writer;

            _output.WriteLine("Server: Waiting for data...");
            var result = await reader.ReadAsync();
            var request = BufferToArray(result.Buffer);
            _output.WriteLine($"Server: Received {request.Length} bytes");

            reader.AdvanceTo(result.Buffer.End);

            var response = new byte[] { (byte)(request[0] + 10), request[1], request[2] };
            await writer.WriteAsync(response);
            await writer.FlushAsync();
            writer.Complete();
        }, TestContext.Current.CancellationToken);

        var clientTask = Task.Run(async () => {
            var writer = pipe1.Writer;
            var reader = pipe2.Reader;

            var request = new byte[] { 100, 101, 102 };
            await writer.WriteAsync(request);
            await writer.FlushAsync();
            writer.Complete();

            var result = await reader.ReadAsync();
            var response = BufferToArray(result.Buffer);
            reader.AdvanceTo(result.Buffer.End);
            return response;
        });

        var response = await clientTask;
        await serverTask;

        Assert.Equal(3, response.Length);
        Assert.Equal(110, response[0]);
    }

    private static byte[] BufferToArray(ReadOnlySequence<byte> buffer) {
        if (buffer.IsSingleSegment) {
            return buffer.FirstSpan.ToArray();
        }

        var data = new byte[buffer.Length];
        var index = 0;
        foreach (var segment in buffer) {
            segment.Span.CopyTo(data.AsSpan(index));
            index += segment.Length;
        }
        return data;
    }
}