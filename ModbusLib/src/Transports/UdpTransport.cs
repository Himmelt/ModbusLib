using System.Net;
using System.Net.Sockets;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Transports;

/// <summary>
/// UDP传输实现
/// </summary>
public class UdpTransport : IModbusTransport
{
    private UdpClient? _udpClient;
    private IPEndPoint? _remoteEndPoint;
    private readonly NetworkConnectionConfig _config;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed = false;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool IsConnected => _udpClient != null && _remoteEndPoint != null;

    public UdpTransport(NetworkConnectionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UdpTransport));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
                return true;

            await DisconnectInternalAsync();

            // 解析主机地址
            IPAddress? ipAddress;
            if (!IPAddress.TryParse(_config.Host, out ipAddress))
            {
                var hostEntry = await Dns.GetHostEntryAsync(_config.Host, cancellationToken);
                ipAddress = hostEntry.AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                if (ipAddress == null)
                    throw new ModbusConnectionException($"无法解析主机地址: {_config.Host}");
            }

            _remoteEndPoint = new IPEndPoint(ipAddress, _config.Port);
            _udpClient = new UdpClient();

            // 配置UDP选项
            _udpClient.Client.ReceiveTimeout = _config.ReceiveTimeout;
            _udpClient.Client.SendTimeout = _config.SendTimeout;
            _udpClient.Client.ReceiveBufferSize = _config.ReceiveBufferSize;
            _udpClient.Client.SendBufferSize = _config.SendBufferSize;

            // UDP是无连接协议，这里只是配置远程端点
            _udpClient.Connect(_remoteEndPoint);

            return true;
        }
        catch (Exception ex)
        {
            await DisconnectInternalAsync();
            throw new ModbusConnectionException($"UDP连接配置失败: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await DisconnectInternalAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private Task DisconnectInternalAsync()
    {
        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
            _remoteEndPoint = null;
        }
        catch
        {
            // 忽略断开连接时的异常
        }
        
        return Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UdpTransport));

        if (!IsConnected)
            throw new ModbusConnectionException("UDP连接未配置");

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var udpClient = _udpClient!;
            var remoteEndPoint = _remoteEndPoint!;

            // 发送请求
            var bytesSent = await udpClient.SendAsync(request, cancellationToken);
            if (bytesSent != request.Length)
            {
                throw new ModbusCommunicationException($"UDP发送不完整，期望{request.Length}字节，实际发送{bytesSent}字节");
            }

            // 接收响应
            var response = await ReceiveResponseAsync(udpClient, cancellationToken);
            return response;
        }
        catch (Exception ex) when (ex is SocketException)
        {
            throw new ModbusCommunicationException($"UDP通信异常: {ex.Message}", ex);
        }
        catch (TimeoutException)
        {
            throw new ModbusTimeoutException("UDP通信超时");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(UdpClient udpClient, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Timeout);

        try
        {
            var result = await udpClient.ReceiveAsync(timeoutCts.Token);
            return result.Buffer;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new ModbusTimeoutException("UDP接收超时");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            DisconnectInternalAsync().Wait(1000);
        }
        catch
        {
            // 忽略释放时的异常
        }

        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await DisconnectAsync();
        }
        catch
        {
            // 忽略释放时的异常
        }

        _semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}