using System.Buffers;
using System.IO.Ports;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Transports;

/// <summary>
/// 串口传输实现
/// </summary>
public class SerialTransport(SerialConnectionConfig config) : IModbusTransport
{
    private SerialPort? _serialPort;
    private readonly SerialConnectionConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool IsConnected => _serialPort?.IsOpen == true;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected)
                return true;

            _serialPort?.Dispose();
            _serialPort = new SerialPort
            {
                PortName = _config.PortName,
                BaudRate = _config.BaudRate,
                Parity = _config.Parity,
                DataBits = _config.DataBits,
                StopBits = _config.StopBits,
                Handshake = _config.Handshake,
                ReadTimeout = _config.ReadTimeout,
                WriteTimeout = _config.WriteTimeout
            };

            await Task.Run(() => _serialPort.Open(), cancellationToken).ConfigureAwait(false);
            
            // 清空输入输出缓冲区
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            return true;
        }
        catch (Exception ex)
        {
            throw new ModbusConnectionException($"串口连接失败: {ex.Message}", ex);
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

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_serialPort?.IsOpen == true)
            {
                await Task.Run(() => _serialPort.Close(), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected)
            throw new ModbusConnectionException("串口未连接");

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var serialPort = _serialPort!;
            
            // 清空输入缓冲区
            serialPort.DiscardInBuffer();

            // 发送请求
            await Task.Run(() => serialPort.Write(request, 0, request.Length), cancellationToken).ConfigureAwait(false);

            // 接收响应
            var response = await ReceiveResponseAsync(serialPort, cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch (TimeoutException)
        {
            throw new ModbusTimeoutException("串口通信超时");
        }
        catch (Exception ex)
        {
            throw new ModbusCommunicationException($"串口通信异常: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(SerialPort serialPort, CancellationToken cancellationToken)
    {
        const int bufferSize = 256;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var responseList = new List<byte>();
        
        try
        {
            var timeout = DateTime.UtcNow.Add(Timeout);
            var lastReceiveTime = DateTime.UtcNow;

            while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
            {
                if (serialPort.BytesToRead > 0)
                {
                    var bytesToRead = Math.Min(serialPort.BytesToRead, bufferSize);
                    var bytesRead = await Task.Run(() => serialPort.Read(buffer, 0, bytesToRead), cancellationToken).ConfigureAwait(false);
                    
                    for (int i = 0; i < bytesRead; i++)
                    {
                        responseList.Add(buffer[i]);
                    }
                    
                    lastReceiveTime = DateTime.UtcNow;
                }
                else
                {
                    // 检查字符间隔超时
                    if (responseList.Count > 0 && 
                        DateTime.UtcNow - lastReceiveTime > TimeSpan.FromMilliseconds(_config.InterCharTimeout))
                    {
                        break;
                    }
                    
                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                }
            }

            if (responseList.Count == 0)
                throw new ModbusTimeoutException("未收到响应数据");

            return responseList.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        if (disposing)
        {
            try
            {
                _serialPort?.Close();
                _serialPort?.Dispose();
            }
            catch
            {
                // 忽略释放时的异常
            }
            
            _semaphore?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        try
        {
            await DisconnectAsync().ConfigureAwait(false);
            _serialPort?.Dispose();
        }
        catch
        {
            // 忽略释放时的异常
        }
        
        _semaphore?.Dispose();
    }
}