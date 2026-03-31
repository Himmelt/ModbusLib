using ModbusLib.Enums;
using ModbusLib.Models;

namespace ModbusLib.Tests.Mocks;

public class MockChannelModbusServer : IDisposable {
    private readonly ChannelSession _session;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private bool _disposed;

    private readonly bool[] _coils = new bool[65536];
    private readonly ushort[] _holdingRegisters = new ushort[65536];
    private readonly ushort[] _inputRegisters = new ushort[65536];
    private readonly bool[] _discreteInputs = new bool[65536];

    public ChannelSession Session => _session;

    public MockChannelModbusServer() {
        _session = new ChannelSession();
    }

    public void Start() {
        if (_serverTask != null) return;
        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(ServerLoopAsync);
    }

    private async Task ServerLoopAsync() {
        var reader = _session.ClientToServer.Reader;
        var writer = _session.ServerToClient.Writer;
        var token = _cts!.Token;

        try {
            while (!token.IsCancellationRequested) {
                var data = await reader.ReadAsync(token);

                if (data.Length == 0) continue;

                ProcessRequest(data, writer);
            }
        } catch (OperationCanceledException) {
        } catch (Exception) {
        } finally {
            try {
                writer.Complete();
            } catch (Exception) {
            }
        }
    }

    private void ProcessRequest(ReadOnlySpan<byte> data, System.Threading.Channels.ChannelWriter<byte[]> writer) {
        if (data.Length < 9) return;

        var transactionId = (ushort)((data[0] << 8) | data[1]);
        var unitId = data[6];
        var functionCode = (ModbusFunction)data[7];

        byte[]? response = null;

        switch (functionCode) {
            case ModbusFunction.ReadCoils:
                response = HandleReadCoils(data, transactionId, unitId);
                break;
            case ModbusFunction.ReadHoldingRegisters:
                response = HandleReadHoldingRegisters(data, transactionId, unitId);
                break;
            case ModbusFunction.WriteSingleCoil:
                response = HandleWriteSingleCoil(data, transactionId, unitId);
                break;
            case ModbusFunction.WriteSingleRegister:
                response = HandleWriteSingleRegister(data, transactionId, unitId);
                break;
            case ModbusFunction.WriteMultipleCoils:
                response = HandleWriteMultipleCoils(data, transactionId, unitId);
                break;
            case ModbusFunction.WriteMultipleRegisters:
                response = HandleWriteMultipleRegisters(data, transactionId, unitId);
                break;
        }

        if (response != null) {
            writer.TryWrite(response);
        }
    }

    private byte[] HandleReadCoils(ReadOnlySpan<byte> data, ushort transactionId, byte unitId) {
        var startAddress = (ushort)((data[8] << 8) | data[9]);
        var quantity = (ushort)((data[10] << 8) | data[11]);

        var byteCount = (quantity + 7) / 8;
        var response = new byte[9 + byteCount];

        response[0] = (byte)(transactionId >> 8);
        response[1] = (byte)(transactionId & 0xFF);
        response[2] = 0x00;
        response[3] = 0x00;
        response[4] = (byte)((3 + byteCount) >> 8);
        response[5] = (byte)(3 + byteCount);
        response[6] = unitId;
        response[7] = (byte)ModbusFunction.ReadCoils;
        response[8] = (byte)byteCount;

        for (var i = 0; i < byteCount; i++) {
            var byteValue = 0;
            for (var j = 0; j < 8; j++) {
                var coilIndex = startAddress + i * 8 + j;
                if (coilIndex < _coils.Length && coilIndex < startAddress + quantity) {
                    if (_coils[coilIndex]) {
                        byteValue |= (1 << j);
                    }
                }
            }
            response[9 + i] = (byte)byteValue;
        }

        return response;
    }

    private byte[] HandleReadHoldingRegisters(ReadOnlySpan<byte> data, ushort transactionId, byte unitId) {
        var startAddress = (ushort)((data[8] << 8) | data[9]);
        var quantity = (ushort)((data[10] << 8) | data[11]);

        var byteCount = (byte)(quantity * 2);
        var response = new byte[9 + byteCount];

        response[0] = (byte)(transactionId >> 8);
        response[1] = (byte)(transactionId & 0xFF);
        response[2] = 0x00;
        response[3] = 0x00;
        response[4] = (byte)((3 + byteCount) >> 8);
        response[5] = (byte)(3 + byteCount);
        response[6] = unitId;
        response[7] = (byte)ModbusFunction.ReadHoldingRegisters;
        response[8] = byteCount;

        for (var i = 0; i < quantity; i++) {
            var registerIndex = startAddress + i;
            var value = registerIndex < _holdingRegisters.Length ? _holdingRegisters[registerIndex] : (ushort)0;
            response[9 + i * 2] = (byte)(value >> 8);
            response[9 + i * 2 + 1] = (byte)(value & 0xFF);
        }

        return response;
    }

    private byte[] HandleWriteSingleCoil(ReadOnlySpan<byte> data, ushort transactionId, byte unitId) {
        var address = (ushort)((data[8] << 8) | data[9]);
        var value = (ushort)((data[10] << 8) | data[11]);

        if (address < _coils.Length) {
            _coils[address] = value == 0xFF00;
        }

        var response = new byte[12];
        data.Slice(0, 8).CopyTo(response);
        response[8] = (byte)(address >> 8);
        response[9] = (byte)(address & 0xFF);
        response[10] = (byte)(value >> 8);
        response[11] = (byte)(value & 0xFF);

        return response;
    }

    private byte[] HandleWriteSingleRegister(ReadOnlySpan<byte> data, ushort transactionId, byte unitId) {
        var address = (ushort)((data[8] << 8) | data[9]);
        var value = (ushort)((data[10] << 8) | data[11]);

        if (address < _holdingRegisters.Length) {
            _holdingRegisters[address] = value;
        }

        var response = new byte[12];
        data.Slice(0, 8).CopyTo(response);
        response[8] = (byte)(address >> 8);
        response[9] = (byte)(address & 0xFF);
        response[10] = (byte)(value >> 8);
        response[11] = (byte)(value & 0xFF);

        return response;
    }

    private byte[] HandleWriteMultipleCoils(ReadOnlySpan<byte> data, ushort transactionId, byte unitId) {
        var startAddress = (ushort)((data[8] << 8) | data[9]);
        var quantity = (ushort)((data[10] << 8) | data[11]);

        if (quantity > 0 && data.Length >= 13) {
            var byteCount = data[12];
            for (var i = 0; i < quantity && (startAddress + i) < _coils.Length; i++) {
                var byteIndex = i / 8;
                var bitIndex = i % 8;
                if (byteIndex < byteCount) {
                    _coils[startAddress + i] = ((data[13 + byteIndex] >> bitIndex) & 1) == 1;
                }
            }
        }

        var response = new byte[12];
        response[0] = (byte)(transactionId >> 8);
        response[1] = (byte)(transactionId & 0xFF);
        response[2] = 0x00;
        response[3] = 0x00;
        response[4] = 0x00;
        response[5] = 0x06;
        response[6] = unitId;
        response[7] = (byte)ModbusFunction.WriteMultipleCoils;
        response[8] = (byte)(startAddress >> 8);
        response[9] = (byte)(startAddress & 0xFF);
        response[10] = (byte)(quantity >> 8);
        response[11] = (byte)(quantity & 0xFF);

        return response;
    }

    private byte[] HandleWriteMultipleRegisters(ReadOnlySpan<byte> data, ushort transactionId, byte unitId) {
        var startAddress = (ushort)((data[8] << 8) | data[9]);
        var quantity = (ushort)((data[10] << 8) | data[11]);

        if (quantity > 0 && data.Length >= 13) {
            for (var i = 0; i < quantity * 2 && (startAddress * 2 + i + 13) < data.Length; i += 2) {
                var registerIndex = startAddress + i / 2;
                if (registerIndex < _holdingRegisters.Length) {
                    _holdingRegisters[registerIndex] = (ushort)((data[13 + i] << 8) | data[14 + i]);
                }
            }
        }

        var response = new byte[12];
        response[0] = (byte)(transactionId >> 8);
        response[1] = (byte)(transactionId & 0xFF);
        response[2] = 0x00;
        response[3] = 0x00;
        response[4] = 0x00;
        response[5] = 0x06;
        response[6] = unitId;
        response[7] = (byte)ModbusFunction.WriteMultipleRegisters;
        response[8] = (byte)(startAddress >> 8);
        response[9] = (byte)(startAddress & 0xFF);
        response[10] = (byte)(quantity >> 8);
        response[11] = (byte)(quantity & 0xFF);

        return response;
    }

    public void SetCoil(ushort address, bool value) {
        if (address < _coils.Length) _coils[address] = value;
    }

    public bool GetCoil(ushort address) {
        return address < _coils.Length && _coils[address];
    }

    public void SetHoldingRegister(ushort address, ushort value) {
        if (address < _holdingRegisters.Length) _holdingRegisters[address] = value;
    }

    public ushort GetHoldingRegister(ushort address) {
        return (ushort)(address < _holdingRegisters.Length ? _holdingRegisters[address] : 0);
    }

    public async Task StopAsync() {
        _cts?.Cancel();
        if (_serverTask != null) {
            try {
                await _serverTask;
            } catch (OperationCanceledException) {
            }
        }
    }

    public void Dispose() {
        if (_disposed) return;
        _cts?.Cancel();
        if (_serverTask != null) {
            try {
                _serverTask.Wait(TimeSpan.FromSeconds(2));
            } catch (AggregateException) {
            }
        }
        _cts?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
