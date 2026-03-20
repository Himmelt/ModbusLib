using FluentModbus;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ModbusLib.Tests.FluentServer;

internal abstract class ModbusRequestHandler : IDisposable {
    private CancellationTokenSource _cts;
    private Task? _task;

    public ModbusRequestHandler(ModbusServer modbusServer, int bufferSize) {
        ModbusServer = modbusServer;
        FrameBuffer = new ModbusFrameBuffer(bufferSize);

        LastRequest = Stopwatch.StartNew();
        IsReady = true;

        _cts = new CancellationTokenSource();
        CancellationToken = _cts.Token;
    }

    public ModbusServer ModbusServer { get; }

    public Stopwatch LastRequest { get; protected set; }

    public int Length { get; protected set; }

    public bool IsReady { get; protected set; }

    public CancellationToken CancellationToken { get; }

    public abstract string DisplayName { get; }

    protected byte UnitIdentifier { get; set; }

    protected ModbusFrameBuffer FrameBuffer { get; }

    protected abstract bool IsResponseRequired { get; }

    public void CancelToken() {
        _cts.Cancel();
    }

    public void WriteResponse() {
        int frameLength;
        Action processingMethod;

        if (!IsResponseRequired)
            return;

        if (!(IsReady && Length > 0))
            throw new Exception("No valid request available.");

        var rawFunctionCode = FrameBuffer.Reader.ReadByte();

        FrameBuffer.Writer.Seek(0, SeekOrigin.Begin);

        if (Enum.IsDefined(typeof(ModbusFunctionCode), rawFunctionCode)) {
            var functionCode = (ModbusFunctionCode)rawFunctionCode;

            try {
                processingMethod = functionCode switch {
                    ModbusFunctionCode.ReadHoldingRegisters => ProcessReadHoldingRegisters,
                    ModbusFunctionCode.WriteMultipleRegisters => ProcessWriteMultipleRegisters,
                    ModbusFunctionCode.ReadCoils => ProcessReadCoils,
                    ModbusFunctionCode.ReadDiscreteInputs => ProcessReadDiscreteInputs,
                    ModbusFunctionCode.ReadInputRegisters => ProcessReadInputRegisters,
                    ModbusFunctionCode.WriteSingleCoil => ProcessWriteSingleCoil,
                    ModbusFunctionCode.WriteSingleRegister => ProcessWriteSingleRegister,
                    ModbusFunctionCode.WriteMultipleCoils => ProcessWriteMultipleCoils,
                    ModbusFunctionCode.ReadWriteMultipleRegisters => ProcessReadWriteMultipleRegisters,
                    _ => () => WriteExceptionResponse(rawFunctionCode, ModbusExceptionCode.IllegalFunction)
                };
            } catch (Exception) {
                processingMethod = () => WriteExceptionResponse(rawFunctionCode, ModbusExceptionCode.ServerDeviceFailure);
            }
        } else {
            processingMethod = () => WriteExceptionResponse(rawFunctionCode, ModbusExceptionCode.IllegalFunction);
        }

        if (ModbusServer.IsAsynchronous) {
            lock (ModbusServer.Lock) {
                frameLength = WriteFrame(processingMethod);
            }
        } else {
            frameLength = WriteFrame(processingMethod);
        }

        OnResponseReady(frameLength);
    }

    internal abstract Task ReceiveRequestAsync();

    protected void Start() {
        if (ModbusServer.IsAsynchronous) {
            _task = Task.Run(async () => {
                while (!CancellationToken.IsCancellationRequested) {
                    await ReceiveRequestAsync();
                }
            }, CancellationToken);
        }
    }

    protected abstract int WriteFrame(Action extendFrame);

    protected abstract void OnResponseReady(int frameLength);

    private void WriteExceptionResponse(ModbusFunctionCode functionCode, ModbusExceptionCode exceptionCode) {
        WriteExceptionResponse((byte)functionCode, exceptionCode);
    }

    private void WriteExceptionResponse(byte rawFunctionCode, ModbusExceptionCode exceptionCode) {
        FrameBuffer.Writer.Write((byte)(ModbusFunctionCode.Error + rawFunctionCode));
        FrameBuffer.Writer.Write((byte)exceptionCode);
    }

    private bool CheckRegisterBounds(ModbusFunctionCode functionCode, ushort address, ushort maxStartingAddress, ushort quantityOfRegisters, ushort maxQuantityOfRegisters) {
        if (ModbusServer.RequestValidator is not null) {
            var result = ModbusServer.RequestValidator(new RequestValidatorArgs {
                UnitIdentifier = UnitIdentifier,
                Address = address,
                QuantityOfRegisters = quantityOfRegisters,
                FunctionCode = functionCode,
                ConnectionName = DisplayName
            });

            if (result > ModbusExceptionCode.OK) {
                WriteExceptionResponse(functionCode, result);
                return false;
            }
        }

        if (address < 0 || address + quantityOfRegisters > maxStartingAddress) {
            WriteExceptionResponse(functionCode, ModbusExceptionCode.IllegalDataAddress);
            return false;
        }

        if (quantityOfRegisters <= 0 || quantityOfRegisters > maxQuantityOfRegisters) {
            WriteExceptionResponse(functionCode, ModbusExceptionCode.IllegalDataValue);
            return false;
        }

        return true;
    }

    private bool WriteCoil(bool value, ushort outputAddress) {
        var bufferByteIndex = outputAddress / 8;
        var bufferBitIndex = outputAddress % 8;

        var coils = ModbusServer.GetCoils(UnitIdentifier);
        var oldValue = coils[bufferByteIndex];
        var newValue = oldValue;

        if (value)
            newValue |= (byte)(1 << bufferBitIndex);
        else
            newValue &= (byte)~(1 << bufferBitIndex);

        coils[bufferByteIndex] = newValue;

        return newValue != oldValue;
    }

    private void ProcessReadHoldingRegisters() {
        var startingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityOfRegisters = FrameBuffer.Reader.ReadUInt16Reverse();

        if (CheckRegisterBounds(ModbusFunctionCode.ReadHoldingRegisters, startingAddress, ModbusServer.MaxHoldingRegisterAddress, quantityOfRegisters, 0x7D)) {
            FrameBuffer.Writer.Write((byte)ModbusFunctionCode.ReadHoldingRegisters);
            FrameBuffer.Writer.Write((byte)(quantityOfRegisters * 2));
            FrameBuffer.Writer.Write(ModbusServer.GetHoldingRegisterBuffer(UnitIdentifier).Slice(startingAddress * 2, quantityOfRegisters * 2).ToArray());
        }
    }

    private void ProcessWriteMultipleRegisters() {
        var startingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityOfRegisters = FrameBuffer.Reader.ReadUInt16Reverse();
        var byteCount = FrameBuffer.Reader.ReadByte();

        if (CheckRegisterBounds(ModbusFunctionCode.WriteMultipleRegisters, startingAddress, ModbusServer.MaxHoldingRegisterAddress, quantityOfRegisters, 0x7B)) {
            var holdingRegisters = ModbusServer.GetHoldingRegisters(UnitIdentifier);
            var newValues = MemoryMarshal.Cast<byte, short>(FrameBuffer.Reader.ReadBytes(byteCount).AsSpan());
            newValues.CopyTo(holdingRegisters[startingAddress..]);

            FrameBuffer.Writer.Write((byte)ModbusFunctionCode.WriteMultipleRegisters);

            if (BitConverter.IsLittleEndian) {
                FrameBuffer.Writer.WriteReverse(startingAddress);
                FrameBuffer.Writer.WriteReverse(quantityOfRegisters);
            } else {
                FrameBuffer.Writer.Write(startingAddress);
                FrameBuffer.Writer.Write(quantityOfRegisters);
            }
        }
    }

    private void ProcessReadCoils() {
        var startingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityOfCoils = FrameBuffer.Reader.ReadUInt16Reverse();

        if (CheckRegisterBounds(ModbusFunctionCode.ReadCoils, startingAddress, ModbusServer.MaxCoilAddress, quantityOfCoils, 0x7D0)) {
            var byteCount = (byte)Math.Ceiling((double)quantityOfCoils / 8);

            var coilBuffer = ModbusServer.GetCoilBuffer(UnitIdentifier);
            var targetBuffer = new byte[byteCount];

            for (int i = 0; i < quantityOfCoils; i++) {
                var sourceByteIndex = (startingAddress + i) / 8;
                var sourceBitIndex = (startingAddress + i) % 8;

                var targetByteIndex = i / 8;
                var targetBitIndex = i % 8;

                var isSet = (coilBuffer[sourceByteIndex] & (1 << sourceBitIndex)) > 0;

                if (isSet)
                    targetBuffer[targetByteIndex] |= (byte)(1 << targetBitIndex);
            }

            FrameBuffer.Writer.Write((byte)ModbusFunctionCode.ReadCoils);
            FrameBuffer.Writer.Write(byteCount);
            FrameBuffer.Writer.Write(targetBuffer);
        }
    }

    private void ProcessReadDiscreteInputs() {
        var startingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityOfInputs = FrameBuffer.Reader.ReadUInt16Reverse();

        if (CheckRegisterBounds(ModbusFunctionCode.ReadDiscreteInputs, startingAddress, ModbusServer.MaxDiscreteInputAddress, quantityOfInputs, 0x7D0)) {
            var byteCount = (byte)Math.Ceiling((double)quantityOfInputs / 8);

            var discreteInputBuffer = ModbusServer.GetDiscreteInputBuffer(UnitIdentifier);
            var targetBuffer = new byte[byteCount];

            for (int i = 0; i < quantityOfInputs; i++) {
                var sourceByteIndex = (startingAddress + i) / 8;
                var sourceBitIndex = (startingAddress + i) % 8;

                var targetByteIndex = i / 8;
                var targetBitIndex = i % 8;

                var isSet = (discreteInputBuffer[sourceByteIndex] & (1 << sourceBitIndex)) > 0;

                if (isSet)
                    targetBuffer[targetByteIndex] |= (byte)(1 << targetBitIndex);
            }

            FrameBuffer.Writer.Write((byte)ModbusFunctionCode.ReadDiscreteInputs);
            FrameBuffer.Writer.Write(byteCount);
            FrameBuffer.Writer.Write(targetBuffer);
        }
    }

    private void ProcessReadInputRegisters() {
        var startingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityOfRegisters = FrameBuffer.Reader.ReadUInt16Reverse();

        if (CheckRegisterBounds(ModbusFunctionCode.ReadInputRegisters, startingAddress, ModbusServer.MaxInputRegisterAddress, quantityOfRegisters, 0x7D)) {
            FrameBuffer.Writer.Write((byte)ModbusFunctionCode.ReadInputRegisters);
            FrameBuffer.Writer.Write((byte)(quantityOfRegisters * 2));
            FrameBuffer.Writer.Write(ModbusServer.GetInputRegisterBuffer(UnitIdentifier).Slice(startingAddress * 2, quantityOfRegisters * 2).ToArray());
        }
    }

    private void ProcessWriteMultipleCoils() {
        const int maxQuantityOfOutputs = 0x07B0;

        var startingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityOfOutputs = FrameBuffer.Reader.ReadUInt16Reverse();
        var byteCount = FrameBuffer.Reader.ReadByte();
        var byteCountFromQuantity = (quantityOfOutputs + 7) / 8;

        if (byteCountFromQuantity != byteCount) {
            WriteExceptionResponse(ModbusFunctionCode.WriteMultipleCoils, ModbusExceptionCode.IllegalDataValue);
            return;
        }

        if (CheckRegisterBounds(ModbusFunctionCode.WriteMultipleCoils, startingAddress, ModbusServer.MaxCoilAddress, quantityOfOutputs, maxQuantityOfOutputs)) {
            var newValues = FrameBuffer.Reader.ReadBytes(byteCount);

            for (var i = 0; i < quantityOfOutputs; i++) {
                byte b = newValues[i / 8];
                int bit = i % 8;
                bool value = (b & (1 << bit)) != 0;
                WriteCoil(value, (ushort)(startingAddress + i));
            }
        }

        FrameBuffer.Writer.Write((byte)ModbusFunctionCode.WriteMultipleCoils);

        if (BitConverter.IsLittleEndian) {
            FrameBuffer.Writer.WriteReverse(startingAddress);
            FrameBuffer.Writer.WriteReverse(quantityOfOutputs);
        } else {
            FrameBuffer.Writer.Write(startingAddress);
            FrameBuffer.Writer.Write(quantityOfOutputs);
        }
    }

    private void ProcessWriteSingleCoil() {
        var outputAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var outputValue = FrameBuffer.Reader.ReadUInt16();

        if (CheckRegisterBounds(ModbusFunctionCode.WriteSingleCoil, outputAddress, ModbusServer.MaxCoilAddress, 1, 1)) {
            if (outputValue != 0x0000 && outputValue != 0x00FF) {
                WriteExceptionResponse(ModbusFunctionCode.WriteSingleCoil, ModbusExceptionCode.IllegalDataValue);
            } else {
                WriteCoil(outputValue == 0x00FF, outputAddress);

                FrameBuffer.Writer.Write((byte)ModbusFunctionCode.WriteSingleCoil);

                if (BitConverter.IsLittleEndian)
                    FrameBuffer.Writer.WriteReverse(outputAddress);
                else
                    FrameBuffer.Writer.Write(outputAddress);

                FrameBuffer.Writer.Write(outputValue);
            }
        }
    }

    private void ProcessWriteSingleRegister() {
        var registerAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var registerValue = FrameBuffer.Reader.ReadInt16();

        if (CheckRegisterBounds(ModbusFunctionCode.WriteSingleRegister, registerAddress, ModbusServer.MaxHoldingRegisterAddress, 1, 1)) {
            var holdingRegisters = ModbusServer.GetHoldingRegisters(UnitIdentifier);
            holdingRegisters[registerAddress] = registerValue;

            FrameBuffer.Writer.Write((byte)ModbusFunctionCode.WriteSingleRegister);

            if (BitConverter.IsLittleEndian)
                FrameBuffer.Writer.WriteReverse(registerAddress);
            else
                FrameBuffer.Writer.Write(registerAddress);

            FrameBuffer.Writer.Write(registerValue);
        }
    }

    private void ProcessReadWriteMultipleRegisters() {
        var readStartingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityToRead = FrameBuffer.Reader.ReadUInt16Reverse();
        var writeStartingAddress = FrameBuffer.Reader.ReadUInt16Reverse();
        var quantityToWrite = FrameBuffer.Reader.ReadUInt16Reverse();
        var writeByteCount = FrameBuffer.Reader.ReadByte();

        if (CheckRegisterBounds(ModbusFunctionCode.ReadWriteMultipleRegisters, readStartingAddress, ModbusServer.MaxHoldingRegisterAddress, quantityToRead, 0x7D)) {
            if (CheckRegisterBounds(ModbusFunctionCode.ReadWriteMultipleRegisters, writeStartingAddress, ModbusServer.MaxHoldingRegisterAddress, quantityToWrite, 0x7B)) {
                var holdingRegisters = ModbusServer.GetHoldingRegisters(UnitIdentifier);

                var writeData = MemoryMarshal.Cast<byte, short>(FrameBuffer.Reader.ReadBytes(writeByteCount).AsSpan());
                writeData.CopyTo(holdingRegisters[writeStartingAddress..]);

                var readData = MemoryMarshal.AsBytes(holdingRegisters.Slice(readStartingAddress, quantityToRead)).ToArray();

                FrameBuffer.Writer.Write((byte)ModbusFunctionCode.ReadWriteMultipleRegisters);
                FrameBuffer.Writer.Write((byte)(quantityToRead * 2));
                FrameBuffer.Writer.Write(readData);
            }
        }
    }

    private bool _disposedValue = false;

    protected virtual void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                if (ModbusServer.IsAsynchronous) {
                    CancelToken();

                    try { _task?.Wait(); } catch (Exception ex) when (ex.InnerException is TaskCanceledException) { }
                }

                FrameBuffer.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(true);
    }
}