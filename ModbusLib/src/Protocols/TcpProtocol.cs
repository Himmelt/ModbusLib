using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Protocols;

/// <summary>
/// TCP协议处理器
/// </summary>
public class TcpProtocol : IModbusProtocol
{
    private ushort _transactionId = 0;
    private readonly object _transactionLock = new object();

    public byte[] BuildRequest(ModbusRequest request)
    {
        var pdu = BuildPdu(request);
        var frame = new byte[7 + pdu.Length]; // MBAP Header (6字节) + SlaveId (1字节) + PDU
        
        ushort currentTransactionId;
        lock (_transactionLock)
        {
            _transactionId = (ushort)((_transactionId + 1) % 65536);
            currentTransactionId = _transactionId;
        }
        
        // 构建MBAP头部
        frame[0] = (byte)(currentTransactionId >> 8);   // 事务ID高字节
        frame[1] = (byte)(currentTransactionId & 0xFF); // 事务ID低字节
        frame[2] = 0x00; // 协议ID高字节 (固定为0)
        frame[3] = 0x00; // 协议ID低字节 (固定为0)
        frame[4] = (byte)((pdu.Length + 1) >> 8);       // 长度高字节 (PDU长度 + 单元ID)
        frame[5] = (byte)((pdu.Length + 1) & 0xFF);     // 长度低字节
        
        // 添加单元ID
        frame[6] = request.SlaveId;
        
        // 复制PDU
        Array.Copy(pdu, 0, frame, 7, pdu.Length);
        
        return frame;
    }

    public ModbusResponse ParseResponse(byte[] response, ModbusRequest request)
    {
        if (response.Length < 6)
            throw new ModbusCommunicationException($"TCP响应长度不足: {response.Length}");

        // 解析MBAP头部
        var transactionId = (ushort)((response[0] << 8) | response[1]);
        var protocolId = (ushort)((response[2] << 8) | response[3]);
        var length = (ushort)((response[4] << 8) | response[5]);
        
        if (protocolId != 0)
            throw new ModbusCommunicationException($"无效的协议ID: {protocolId}");
            
        if (response.Length < 6 + length)
            throw new ModbusCommunicationException($"TCP响应数据不完整，期望{6 + length}字节，实际{response.Length}字节");

        var slaveId = response[6];
        var functionCode = response[7];
        
        // 检查是否为异常响应
        if ((functionCode & 0x80) != 0)
        {
            if (response.Length < 9)
                throw new ModbusCommunicationException("TCP异常响应长度不足");
                
            var originalFunction = (ModbusFunction)(functionCode & 0x7F);
            var exceptionCode = (ModbusExceptionCode)response[8];
            
            return ModbusResponse.CreateError(slaveId, originalFunction, exceptionCode);
        }

        // 解析正常响应数据
        var dataLength = length - 2; // 减去单元ID和功能码
        var data = new byte[dataLength];
        if (dataLength > 0)
        {
            Array.Copy(response, 8, data, 0, dataLength);
        }

        return new ModbusResponse(slaveId, (ModbusFunction)functionCode, data)
        {
            RawData = response
        };
    }

    public bool ValidateResponse(byte[] response)
    {
        if (response.Length < 6)
            return false;
            
        var protocolId = (ushort)((response[2] << 8) | response[3]);
        var length = (ushort)((response[4] << 8) | response[5]);
        
        return protocolId == 0 && response.Length >= 6 + length;
    }

    public int CalculateExpectedResponseLength(ModbusRequest request)
    {
        var pduLength = request.Function switch
        {
            ModbusFunction.ReadCoils => 2 + (request.Quantity + 7) / 8, // Function + ByteCount + Data
            ModbusFunction.ReadDiscreteInputs => 2 + (request.Quantity + 7) / 8,
            ModbusFunction.ReadHoldingRegisters => 2 + request.Quantity * 2,
            ModbusFunction.ReadInputRegisters => 2 + request.Quantity * 2,
            ModbusFunction.WriteSingleCoil => 5, // Function + Address + Value
            ModbusFunction.WriteSingleRegister => 5,
            ModbusFunction.WriteMultipleCoils => 5, // Function + Address + Quantity
            ModbusFunction.WriteMultipleRegisters => 5,
            ModbusFunction.ReadWriteMultipleRegisters => 2 + request.Quantity * 2,
            _ => throw new NotSupportedException($"不支持的功能码: {request.Function}")
        };
        
        return 6 + 1 + pduLength; // MBAP Header + SlaveId + PDU
    }

    private static byte[] BuildPdu(ModbusRequest request)
    {
        return request.Function switch
        {
            ModbusFunction.ReadCoils => BuildReadPdu(request),
            ModbusFunction.ReadDiscreteInputs => BuildReadPdu(request),
            ModbusFunction.ReadHoldingRegisters => BuildReadPdu(request),
            ModbusFunction.ReadInputRegisters => BuildReadPdu(request),
            ModbusFunction.WriteSingleCoil => BuildWriteSingleCoilPdu(request),
            ModbusFunction.WriteSingleRegister => BuildWriteSingleRegisterPdu(request),
            ModbusFunction.WriteMultipleCoils => BuildWriteMultipleCoilsPdu(request),
            ModbusFunction.WriteMultipleRegisters => BuildWriteMultipleRegistersPdu(request),
            ModbusFunction.ReadWriteMultipleRegisters => BuildReadWriteMultipleRegistersPdu(request),
            _ => throw new NotSupportedException($"不支持的功能码: {request.Function}")
        };
    }

    private static byte[] BuildReadPdu(ModbusRequest request)
    {
        return
        [
            (byte)request.Function,
            (byte)(request.StartAddress >> 8),
            (byte)(request.StartAddress & 0xFF),
            (byte)(request.Quantity >> 8),
            (byte)(request.Quantity & 0xFF)
        ];
    }

    private static byte[] BuildWriteSingleCoilPdu(ModbusRequest request)
    {
        if (request.Data == null || request.Data.Length < 1)
            throw new ArgumentException("WriteSingleCoil需要数据");

        var value = request.Data[0] != 0 ? (ushort)0xFF00 : (ushort)0x0000;
        
        return
        [
            (byte)request.Function,
            (byte)(request.StartAddress >> 8),
            (byte)(request.StartAddress & 0xFF),
            (byte)(value >> 8),
            (byte)(value & 0xFF)
        ];
    }

    private static byte[] BuildWriteSingleRegisterPdu(ModbusRequest request)
    {
        if (request.Data == null || request.Data.Length < 2)
            throw new ArgumentException("WriteSingleRegister需要2字节数据");

        var value = (ushort)((request.Data[0] << 8) | request.Data[1]);
        
        return
        [
            (byte)request.Function,
            (byte)(request.StartAddress >> 8),
            (byte)(request.StartAddress & 0xFF),
            (byte)(value >> 8),
            (byte)(value & 0xFF)
        ];
    }

    private static byte[] BuildWriteMultipleCoilsPdu(ModbusRequest request)
    {
        if (request.Data == null)
            throw new ArgumentException("WriteMultipleCoils需要数据");

        var byteCount = (byte)request.Data.Length;
        var pdu = new byte[5 + byteCount];
        
        pdu[0] = (byte)request.Function;
        pdu[1] = (byte)(request.StartAddress >> 8);
        pdu[2] = (byte)(request.StartAddress & 0xFF);
        pdu[3] = (byte)(request.Quantity >> 8);
        pdu[4] = (byte)(request.Quantity & 0xFF);
        pdu[5] = byteCount;
        
        Array.Copy(request.Data, 0, pdu, 6, byteCount);
        
        return pdu;
    }

    private static byte[] BuildWriteMultipleRegistersPdu(ModbusRequest request)
    {
        if (request.Data == null)
            throw new ArgumentException("WriteMultipleRegisters需要数据");

        var byteCount = (byte)request.Data.Length;
        var pdu = new byte[6 + byteCount];
        
        pdu[0] = (byte)request.Function;
        pdu[1] = (byte)(request.StartAddress >> 8);
        pdu[2] = (byte)(request.StartAddress & 0xFF);
        pdu[3] = (byte)(request.Quantity >> 8);
        pdu[4] = (byte)(request.Quantity & 0xFF);
        pdu[5] = byteCount;
        
        Array.Copy(request.Data, 0, pdu, 6, byteCount);
        
        return pdu;
    }

    private static byte[] BuildReadWriteMultipleRegistersPdu(ModbusRequest request)
    {
        if (request.Data == null || request.Data.Length < 4)
            throw new ArgumentException("ReadWriteMultipleRegisters需要额外参数数据");

        // 数据格式: [写入起始地址2字节] + [写入数量2字节] + [写入数据...]
        var writeStartAddress = (ushort)((request.Data[0] << 8) | request.Data[1]);
        var writeQuantity = (ushort)((request.Data[2] << 8) | request.Data[3]);
        var writeData = new byte[request.Data.Length - 4];
        Array.Copy(request.Data, 4, writeData, 0, writeData.Length);

        var byteCount = (byte)writeData.Length;
        var pdu = new byte[10 + byteCount];
        
        pdu[0] = (byte)request.Function;
        pdu[1] = (byte)(request.StartAddress >> 8);
        pdu[2] = (byte)(request.StartAddress & 0xFF);
        pdu[3] = (byte)(request.Quantity >> 8);
        pdu[4] = (byte)(request.Quantity & 0xFF);
        pdu[5] = (byte)(writeStartAddress >> 8);
        pdu[6] = (byte)(writeStartAddress & 0xFF);
        pdu[7] = (byte)(writeQuantity >> 8);
        pdu[8] = (byte)(writeQuantity & 0xFF);
        pdu[9] = byteCount;
        
        Array.Copy(writeData, 0, pdu, 10, byteCount);
        
        return pdu;
    }
}