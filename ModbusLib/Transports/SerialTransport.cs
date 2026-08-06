using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;
using ModbusLib.Utils;
using System.Buffers;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace ModbusLib.Transports;

/// <summary>
/// 串口传输实现
/// </summary>
public sealed class SerialTransport : IModbusTransport {

    private readonly SerialConfig _config;
    private readonly ProtocolType _protocol;
    private bool _disposed;
    private SerialPort? _serialPort;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// 使用 RTU 协议帧创建传输
    /// </summary>
    public SerialTransport(SerialConfig config) : this(config, ProtocolType.Rtu) { }

    /// <summary>
    /// 创建传输，<paramref name="protocol"/> 决定响应帧的解析方式（MBAP 或 RTU）
    /// </summary>
    public SerialTransport(SerialConfig config, ProtocolType protocol) {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _protocol = protocol;
    }

    public int Timeout { get; set; } = -1;
    public bool IsConnected => _serialPort?.IsOpen == true;

    public async Task<bool> ConnectAsync(CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try {
            await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException($"串口 {_config.PortName} 获取通信锁超时");
        }

        try {
            if (IsConnected) return true;

            _serialPort?.Dispose();
            _serialPort = new SerialPort {
                PortName = _config.PortName,
                BaudRate = _config.BaudRate,
                Parity = _config.Parity,
                DataBits = _config.DataBits,
                StopBits = _config.StopBits,
                Handshake = _config.Handshake,
                ReadTimeout = _config.ReadTimeout,
                WriteTimeout = _config.WriteTimeout
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            await Task.Run(_serialPort.Open, cts.Token).ConfigureAwait(false);

            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            return true;
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            _serialPort?.Dispose();
            _serialPort = null;
            throw;
        } catch (Exception ex) {
            // 打开失败时释放串口对象，避免占用 COM 端口名等资源
            _serialPort?.Dispose();
            _serialPort = null;
            throw new ModbusConnectionException($"串口 {_config.PortName} 连接失败: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancelToken = default) {
        if (_disposed) return;

        try {
            await _lock.WaitAsync(cancelToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            return;
        }
        try {
            if (_serialPort?.IsOpen == true) {
                _serialPort.Close();
            }
        } finally {
            _lock.Release();
        }
    }

    public async Task<byte[]> SendReceiveAsync(byte[] request, CancellationToken cancelToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        using var opCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        if (Timeout >= 0) opCts.CancelAfter(Timeout);

        try {
            await _lock.WaitAsync(opCts.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException($"串口 {_config.PortName} 获取通信锁超时");
        }

        try {
            if (_serialPort?.IsOpen != true) throw new ModbusConnectionException($"串口 {_config.PortName} 未连接");

            var serialPort = _serialPort;
            serialPort.DiscardInBuffer();

            // 发送阶段（受 WriteTimeout 限制），使用 BaseStream 异步 I/O 以支持取消
            using (var writeCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.WriteTimeout >= 0) writeCts.CancelAfter(_config.WriteTimeout);
                await serialPort.BaseStream.WriteAsync(request.AsMemory(), writeCts.Token).ConfigureAwait(false);
            }

            // 接收阶段（受 ReadTimeout 限制）
            using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(opCts.Token)) {
                if (_config.ReadTimeout >= 0) readCts.CancelAfter(_config.ReadTimeout);
                return await ReceiveResponseAsync(readCts.Token).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) when (cancelToken.IsCancellationRequested) {
            throw; // 保留用户取消语义
        } catch (OperationCanceledException) {
            throw new ModbusTimeoutException($"串口 {_config.PortName} 通信超时，操作已取消");
        } catch (ModbusConnectionException) {
            throw;
        } catch (Exception ex) {
            throw new ModbusCommunicationException($"串口 {_config.PortName} 通信异常: {ex.Message}", ex);
        } finally {
            _lock.Release();
        }
    }

    /// <summary>
    /// 接收一帧完整响应：优先按协议精确计算帧长度（MBAP 长度字段或 RTU 字节计数字段），
    /// 以字符间隔静默时间作为兜底，避免响应被过早截断。
    /// </summary>
    private async Task<byte[]> ReceiveResponseAsync(CancellationToken cancelToken) {
        const int bufferSize = 256;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var response = new List<byte>(32);

        try {
            var serialPort = _serialPort!;
            var lastReceiveTime = DateTime.UtcNow;
            var interCharTimeout = GetDefaultInterCharTimeout(_config.BaudRate);

            while (!cancelToken.IsCancellationRequested) {
                if (serialPort.BytesToRead > 0) {
                    var bytesToRead = Math.Min(serialPort.BytesToRead, bufferSize);
                    var bytesRead = await serialPort.BaseStream.ReadAsync(buffer.AsMemory(0, bytesToRead), cancelToken).ConfigureAwait(false);
                    for (int i = 0; i < bytesRead; i++) {
                        response.Add(buffer[i]);
                    }
                    if (bytesRead > 0) lastReceiveTime = DateTime.UtcNow;
                } else if (response.Count > 0) {
                    // 帧已按协议确定完整长度，直接结束
                    if (ModbusFrameParser.TryGetResponseFrameLength(CollectionsMarshal.AsSpan(response), _protocol) is int expected
                        && response.Count >= expected) {
                        break;
                    }

                    // 超过字符间隔静默时间仍未收完，按当前数据结束（后续CRC/长度校验会兜底）
                    if (DateTime.UtcNow - lastReceiveTime >= interCharTimeout) {
                        break;
                    }

                    await Task.Delay(1, cancelToken).ConfigureAwait(false);
                } else {
                    await Task.Delay(1, cancelToken).ConfigureAwait(false);
                }
            }

            if (response.Count == 0) throw new ModbusTimeoutException($"串口 接收超时，未收到响应数据");

            return [.. response];
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// 未配置 InterCharTimeout 时，依据波特率推导 3.5 个字符时间的静默间隔。
    /// </summary>
    private static TimeSpan GetDefaultInterCharTimeout(int baudRate) {
        if (baudRate <= 0) return TimeSpan.FromMilliseconds(10);
        // 每个字符约 11 位（1起始 + 8数据 + 1校验 + 1停止）
        var charTimeMs = 11.0 / baudRate * 1000.0;
        var ms = Math.Max(2, Math.Ceiling(charTimeMs * 3.5));
        return TimeSpan.FromMilliseconds(ms);
    }

    public void Dispose() {
        if (_disposed) return;

        try {
            _serialPort?.Close();
            _serialPort?.Dispose();
        } catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException) { }
        _serialPort = null;
        _lock.Dispose();

        _disposed = true;
    }

    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
