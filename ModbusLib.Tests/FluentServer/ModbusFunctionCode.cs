namespace FluentModbus;

public enum ModbusFunctionCode : byte {
    ReadHoldingRegisters = 0x03,
    WriteMultipleRegisters = 0x10,
    ReadCoils = 0x01,
    ReadDiscreteInputs = 0x02,
    ReadInputRegisters = 0x04,
    WriteSingleCoil = 0x05,
    WriteSingleRegister = 0x06,
    ReadExceptionStatus = 0x07,
    WriteMultipleCoils = 0x0F,
    ReadFileRecord = 0x14,
    WriteFileRecord = 0x15,
    MaskWriteRegister = 0x16,
    ReadWriteMultipleRegisters = 0x17,
    ReadFifoQueue = 0x18,
    Error = 0x80
}