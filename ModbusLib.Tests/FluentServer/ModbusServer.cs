using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FluentModbus;

public readonly struct RequestValidatorArgs {
    public byte UnitIdentifier { get; init; }
    public ModbusFunctionCode FunctionCode { get; init; }
    public ushort Address { get; init; }
    public ushort QuantityOfRegisters { get; init; }
    public string ConnectionName { get; init; }
}

public abstract class ModbusServer : IDisposable {
    private readonly Dictionary<byte, byte[]> _inputRegisterBufferMap = [];
    private readonly Dictionary<byte, byte[]> _holdingRegisterBufferMap = [];
    private readonly Dictionary<byte, byte[]> _coilBufferMap = [];
    private readonly Dictionary<byte, byte[]> _discreteInputBufferMap = [];

    private readonly int _inputRegisterSize;
    private readonly int _holdingRegisterSize;
    private readonly int _coilSize;
    private readonly int _discreteInputSize;

    private readonly List<byte> _unitIdentifiers = [];

    public ModbusServer(bool isAsynchronous, ILogger logger) {
        Lock = this;
        IsAsynchronous = isAsynchronous;
        Logger = logger;

        MaxInputRegisterAddress = ushort.MaxValue;
        MaxHoldingRegisterAddress = ushort.MaxValue;
        MaxCoilAddress = ushort.MaxValue;
        MaxDiscreteInputAddress = ushort.MaxValue;

        _inputRegisterSize = (MaxInputRegisterAddress + 1) * 2;
        _holdingRegisterSize = (MaxHoldingRegisterAddress + 1) * 2;
        _coilSize = (MaxCoilAddress + 1 + 7) / 8;
        _discreteInputSize = (MaxDiscreteInputAddress + 1 + 7) / 8;

        UnitIdentifiers = _unitIdentifiers.AsReadOnly();
    }

    public IReadOnlyList<byte> UnitIdentifiers { get; }

    public object Lock { get; }

    public bool IsAsynchronous { get; }

    public ushort MaxInputRegisterAddress { get; }

    public ushort MaxHoldingRegisterAddress { get; }

    public ushort MaxCoilAddress { get; }

    public ushort MaxDiscreteInputAddress { get; }

    public Func<RequestValidatorArgs, ModbusExceptionCode>? RequestValidator { get; set; }

    internal protected ILogger Logger { get; }

    private protected CancellationTokenSource CTS { get; private set; } = new CancellationTokenSource();

    internal bool IsSingleZeroUnitMode => UnitIdentifiers.Count == 1 && UnitIdentifiers[0] == 0;

    public Span<short> GetInputRegisters(byte unitIdentifier = 0) {
        return MemoryMarshal.Cast<byte, short>(GetInputRegisterBuffer(unitIdentifier));
    }

    public Span<T> GetInputRegisterBuffer<T>(byte unitIdentifier = 0) where T : unmanaged {
        return MemoryMarshal.Cast<byte, T>(GetInputRegisterBuffer(unitIdentifier));
    }

    public Span<byte> GetInputRegisterBuffer(byte unitIdentifier = 0) {
        return Find(unitIdentifier, _inputRegisterBufferMap);
    }

    public Span<short> GetHoldingRegisters(byte unitIdentifier = 0) {
        return MemoryMarshal.Cast<byte, short>(GetHoldingRegisterBuffer(unitIdentifier));
    }

    public Span<T> GetHoldingRegisterBuffer<T>(byte unitIdentifier = 0) where T : unmanaged {
        return MemoryMarshal.Cast<byte, T>(GetHoldingRegisterBuffer(unitIdentifier));
    }

    public Span<byte> GetHoldingRegisterBuffer(byte unitIdentifier = 0) {
        return Find(unitIdentifier, _holdingRegisterBufferMap);
    }

    public Span<byte> GetCoils(byte unitIdentifier = 0) {
        return GetCoilBuffer(unitIdentifier);
    }

    public Span<T> GetCoilBuffer<T>(byte unitIdentifier = 0) where T : unmanaged {
        return MemoryMarshal.Cast<byte, T>(GetCoilBuffer(unitIdentifier));
    }

    public Span<byte> GetCoilBuffer(byte unitIdentifier = 0) {
        return Find(unitIdentifier, _coilBufferMap);
    }

    public Span<byte> GetDiscreteInputs(byte unitIdentifier = 0) {
        return GetDiscreteInputBuffer(unitIdentifier);
    }

    public Span<T> GetDiscreteInputBuffer<T>(byte unitIdentifier = 0) where T : unmanaged {
        return MemoryMarshal.Cast<byte, T>(GetDiscreteInputBuffer(unitIdentifier));
    }

    public Span<byte> GetDiscreteInputBuffer(byte unitIdentifier = 0) {
        return Find(unitIdentifier, _discreteInputBufferMap);
    }

    public void AddUnit(byte unitIdentifier) {
        if (!_unitIdentifiers.Contains(unitIdentifier)) {
            _unitIdentifiers.Add(unitIdentifier);
            _inputRegisterBufferMap[unitIdentifier] = new byte[_inputRegisterSize];
            _holdingRegisterBufferMap[unitIdentifier] = new byte[_holdingRegisterSize];
            _coilBufferMap[unitIdentifier] = new byte[_coilSize];
            _discreteInputBufferMap[unitIdentifier] = new byte[_discreteInputSize];
        }
    }

    public void RemoveUnit(byte unitIdentifier) {
        if (_unitIdentifiers.Contains(unitIdentifier)) {
            _inputRegisterBufferMap.Remove(unitIdentifier);
            _holdingRegisterBufferMap.Remove(unitIdentifier);
            _coilBufferMap.Remove(unitIdentifier);
            _discreteInputBufferMap.Remove(unitIdentifier);
            _unitIdentifiers.Remove(unitIdentifier);
        }
    }

    private Span<byte> Find(byte unitIdentifier, Dictionary<byte, byte[]> map) {
        if (!map.TryGetValue(unitIdentifier, out var buffer))
            throw new KeyNotFoundException($"No unit found for unit identifier {unitIdentifier}.");

        return buffer;
    }

    public virtual void Stop() {
        StopProcessing();
    }

    protected virtual void StopProcessing() {
        CTS?.Cancel();
    }

    protected virtual void StartProcessing() {
        CTS = new CancellationTokenSource();
    }

    protected abstract void ProcessRequests();

    public void Dispose() {
        Stop();
        CTS.Dispose();
    }
}