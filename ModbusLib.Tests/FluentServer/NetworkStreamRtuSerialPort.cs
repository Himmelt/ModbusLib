using System.Net.Sockets;

namespace ModbusLib.Tests.FluentServer;

/// <summary>
/// A small adapter that exposes a TcpClient/NetworkStream as an RTU "serial port"
/// compatible with FluentModbus's ModbusRtuServer (read/write/timeout).
/// It calls onReadCallback whenever a successful read (>0 bytes) happens so the
/// caller can update last-activity timestamps.
/// </summary>
public class NetworkStreamRtuSerialPort : IModbusRtuSerialPort, IDisposable {
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private bool _disposed;
    private readonly Action? _onRead;

    /// <summary>
    /// Creates a new instance of the <see cref="NetworkStreamRtuSerialPort"/> class.
    /// </summary>
    /// <param name="client">The TCP client to wrap.</param>
    /// <param name="onReadCallback">Callback invoked whenever a successful read occurs.</param>
    public NetworkStreamRtuSerialPort(TcpClient client, Action onReadCallback) {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = client.GetStream();
        _onRead = onReadCallback;

        PortName = ((System.Net.IPEndPoint?)client.Client.RemoteEndPoint)?.ToString() ?? "tcp";
    }

    /// <summary>
    /// Gets the port name (IP endpoint) of the TCP client.
    /// </summary>
    public string PortName { get; }

    /// <summary>
    /// Gets a value indicating whether the TCP client is connected.
    /// </summary>
    public bool IsOpen => !_disposed && _client.Connected;

    /// <summary>
    /// Gets or sets the read timeout in milliseconds.
    /// </summary>
    public int ReadTimeout { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the write timeout in milliseconds.
    /// </summary>
    public int WriteTimeout { get; set; } = 1000;

    /// <summary>
    /// Opens the connection (no-op for NetworkStream).
    /// </summary>
    public void Open() {
        // nothing to do for NetworkStream
    }

    /// <summary>
    /// Closes the connection.
    /// </summary>
    public void Close() {
        try { _stream.Close(); } catch { }
        try { _client.Close(); } catch { }
    }

    /// <summary>
    /// Reads data asynchronously from the network stream.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The offset in the buffer to start reading.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of bytes read.</returns>
    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        if (_disposed) return 0;

        // We'll implement timeout by a Task.WhenAny with a delay.
        var readTask = _stream.ReadAsync(buffer, offset, count, cancellationToken);

        if (ReadTimeout != Timeout.Infinite) {
            var delay = Task.Delay(ReadTimeout, cancellationToken);
            var completed = await Task.WhenAny(readTask, delay).ConfigureAwait(false);
            if (completed == delay) {
                // emulate timeout -> throw so upstream behaves like serial timeout
                throw new TimeoutException("Read timed out");
            }
        }

        var bytesRead = await readTask.ConfigureAwait(false);

        if (bytesRead > 0)
            _onRead?.Invoke();

        return bytesRead;
    }

    /// <summary>
    /// Writes data synchronously to the network stream.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <param name="offset">The offset in the buffer to start writing.</param>
    /// <param name="count">The number of bytes to write.</param>
    public void Write(byte[] buffer, int offset, int count) {
        ObjectDisposedException.ThrowIf(_disposed, nameof(NetworkStreamRtuSerialPort));
        try {
            _stream.WriteTimeout = WriteTimeout;
            _stream.Write(buffer, offset, count);
            _stream.Flush();
        } catch {
            // propagate so upper layers will cancel/close
            throw;
        }
    }

    /// <summary>
    /// Disposes the resources used by the NetworkStreamRtuSerialPort.
    /// </summary>
    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        try { _stream.Dispose(); } catch { }
        try { _client.Dispose(); } catch { }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Reads data synchronously from the network stream.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The offset in the buffer to start reading.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The number of bytes read.</returns>
    public int Read(byte[] buffer, int offset, int count) {
        if (_disposed) return 0;

        var timeoutTask = Task.Delay(ReadTimeout, CancellationToken.None);
        var readTask = Task.Run(() => _stream.Read(buffer, offset, count));

        if (Task.WhenAny(readTask, timeoutTask).Result == timeoutTask) {
            throw new TimeoutException("Read timed out");
        }

        var bytesRead = readTask.Result;

        if (bytesRead > 0)
            _onRead?.Invoke();

        return bytesRead;
    }

    /// <summary>
    /// Writes data asynchronously to the network stream.
    /// </summary>
    /// <param name="buffer">The buffer to write from.</param>
    /// <param name="offset">The offset in the buffer to start writing.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) {
        throw new NotImplementedException();
    }
}