using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using System.Buffers;
using System.IO.Ports;

namespace ModbusLib.Transports;

/// <summary>
/// 串口传输实现
/// </summary>
public sealed class SerialTransport(SerialConfig config) : IModbusTransport {

    private bool _disposed;
    private SerialPort? _serialPort;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public int Timeout { get; set; } = -1;
    public bool IsConnected => _serialPort?.IsOpen == true;

    public async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        try {
            if (IsConnected) return true;

            _serialPort?.Dispose();
            _serialPort = new SerialPort {
                PortName = config.PortName,
                BaudRate = config.BaudRate,
                Parity = config.Parity,
                DataBits = config.DataBits,
                StopBits = config.StopBits,
                Handshake = config.Handshake,
                ReadTimeout = config.ReadTimeout,
                WriteTimeout = config.WriteTimeout
            };

            await Task.Run(() => _serialPort.Open(), cancelToken).ConfigureAwait(false);

            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            return true;
        } catch (Exception ex) {
            throw new ModbusConnectionException($"串口 {config.PortName} 连接失败: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancelToken = default) {
        if (_disposed) return;

        await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        try {
            if (_serialPort?.IsOpen == true) {
                await Task.Run(_serialPort.Close, cancelToken).ConfigureAwait(false);
            }
        } finally {
            _lock.Release();
        }
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsConnected) throw new ModbusConnectionException($"串口 {_serialPort?.PortName} 未连接");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        cts.CancelAfter(Timeout);

        await _lock.WaitAsync(cts.Token).ConfigureAwait(false);
        try {
            var serialPort = _serialPort!;
            serialPort.DiscardInBuffer();
            await Task.Run(() => serialPort.Write(request, 0, request.Length), cts.Token).ConfigureAwait(false);
            return await ReceiveResponseAsync(cts.Token).ConfigureAwait(false);
        } catch (TimeoutException) {
            throw new ModbusTimeoutException($"串口 {_serialPort?.PortName} 通信超时");
        } catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancelToken.IsCancellationRequested) {
            throw new ModbusTimeoutException($"串口 {_serialPort?.PortName} 通信超时，操作已取消");
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"串口 {_serialPort?.PortName} 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        const int bufferSize = 256;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var responseList = new List<byte>();

        try {
            var lastReceiveTime = DateTime.UtcNow;
            var serialPort = _serialPort!;

            while (!cancelToken.IsCancellationRequested) {
                if (serialPort.BytesToRead > 0) {
                    var bytesToRead = Math.Min(serialPort.BytesToRead, bufferSize);
                    var bytesRead = await Task.Run(() => serialPort.Read(buffer, 0, bytesToRead), cancelToken).ConfigureAwait(false);

                    for (int i = 0; i < bytesRead; i++) {
                        responseList.Add(buffer[i]);
                    }

                    lastReceiveTime = DateTime.UtcNow;
                } else {
                    // 检查字符间隔超时
                    if (config.InterCharTimeout > 0 && responseList.Count > 0 &&
                        DateTime.UtcNow - lastReceiveTime > TimeSpan.FromMilliseconds(config.InterCharTimeout)) {
                        break;
                    }
                    await Task.Delay(1, cancelToken).ConfigureAwait(false);
                }
            }

            if (responseList.Count == 0) throw new ModbusTimeoutException($"串口 接收超时，未收到响应数据");

            return [.. responseList];
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose() {
        if (_disposed) return;

        try {
            _serialPort?.Close();
            _serialPort?.Dispose();
        } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) { }
        _lock.Dispose();

        _disposed = true;
    }

    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }
}