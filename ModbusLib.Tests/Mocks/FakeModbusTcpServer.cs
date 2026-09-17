using System.Net;
using System.Net.Sockets;

namespace ModbusLib.Tests.Mocks;

/// <summary>
/// 最小 Modbus TCP 假对端：可启停（支持在同一端口"设备重启"）、可统计收到的请求数、
/// 可让前 N 个请求只收不回以制造客户端超时。用于验证客户端的断线/重连自愈能力。
/// </summary>
public sealed class FakeModbusTcpServer : IDisposable {

    private readonly int _requestedPort;
    private readonly List<TcpClient> _clients = [];
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private int _requestsSeen;
    private int _connectionsAccepted;
    private int _ignoreRemaining;
    private bool _disposed;

    /// <param name="port">监听端口；0 表示由系统分配（默认用法，避免测试间端口冲突）。</param>
    public FakeModbusTcpServer(int port = 0) {
        _requestedPort = port;
    }

    /// <summary>实际监听端口（<see cref="Start"/> 之后有效）。</summary>
    public int Port { get; private set; }

    /// <summary>累计收到的请求帧数。</summary>
    public int RequestsSeen => Volatile.Read(ref _requestsSeen);

    /// <summary>累计接受的连接数（用于断言"是否真的发生了重连 / 是否擅自建连"）。</summary>
    public int ConnectionsAccepted => Volatile.Read(ref _connectionsAccepted);

    /// <summary>前 N 个请求只接收不回帧（<see cref="int.MaxValue"/> 表示一直不回），用于制造超时。</summary>
    public int IgnoreFirstRequests {
        get => Volatile.Read(ref _ignoreRemaining);
        set => Volatile.Write(ref _ignoreRemaining, value);
    }

    public void Start() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_listener is not null) return;

        // 端口传 0 时由系统分配，但重启（模拟设备重启）必须复用首次分配到的端口
        var port = _requestedPort != 0 ? _requestedPort : Port;
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();

        _listener = listener;
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(listener, _cts.Token));
    }

    /// <summary>停止监听并关闭已接受的连接；之后可在同一端口再次 <see cref="Start"/>（模拟设备重启）。</summary>
    public void Stop() {
        _cts?.Cancel();

        foreach (var client in _clients) {
            try {
                client.Client.Close();
            } catch (Exception ex) when (ex is SocketException or ObjectDisposedException) {
            }
            client.Dispose();
        }
        _clients.Clear();

        try {
            _listener?.Stop();
        } catch (Exception ex) when (ex is SocketException or ObjectDisposedException) {
        }
        _listener = null;

        // 等待 accept 循环退出，避免"新的监听已建立、旧监听仍在排队"造成假象
        try {
            _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
        } catch (AggregateException) {
        }
        _acceptLoop = null;

        _cts?.Dispose();
        _cts = null;
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancelToken) {
        while (!cancelToken.IsCancellationRequested) {
            TcpClient client;
            try {
                client = await listener.AcceptTcpClientAsync(cancelToken).ConfigureAwait(false);
            } catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException) {
                return;
            }

            lock (_clients) {
                _clients.Add(client);
            }
            Interlocked.Increment(ref _connectionsAccepted);
            _ = Task.Run(() => ServeAsync(client, cancelToken));
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken cancelToken) {
        var stream = client.GetStream();
        var buffer = new byte[512];
        try {
            while (!cancelToken.IsCancellationRequested) {
                var read = await stream.ReadAsync(buffer, cancelToken).ConfigureAwait(false);
                if (read == 0) return;

                Interlocked.Increment(ref _requestsSeen);
                if (Interlocked.Decrement(ref _ignoreRemaining) >= 0) continue;   // 只收不回：制造客户端超时

                var unitId = buffer[6];
                var quantity = (ushort)((buffer[10] << 8) | buffer[11]);
                await stream.WriteAsync(BuildReadHoldingRegistersResponse(buffer, unitId, quantity), cancelToken).ConfigureAwait(false);
            }
        } catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException or ObjectDisposedException) {
        }
    }

    /// <summary>回一个读保持寄存器（0x03）响应，数据为 0x10,0x11,0x12…，事务号原样回显。</summary>
    private static byte[] BuildReadHoldingRegistersResponse(byte[] request, byte unitId, ushort quantity) {
        var byteCount = quantity * 2;
        var frame = new byte[9 + byteCount];
        frame[0] = request[0];                                 // 事务号高字节
        frame[1] = request[1];                                 // 事务号低字节
        frame[4] = (byte)((3 + byteCount) >> 8);               // 长度高字节
        frame[5] = (byte)((3 + byteCount) & 0xFF);             // 长度低字节
        frame[6] = unitId;
        frame[7] = 0x03;
        frame[8] = (byte)byteCount;
        for (var i = 0; i < byteCount; i++) {
            frame[9 + i] = (byte)(0x10 + i);
        }
        return frame;
    }

    public void Dispose() {
        if (_disposed) return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
