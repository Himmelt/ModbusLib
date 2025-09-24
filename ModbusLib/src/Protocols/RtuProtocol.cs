using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Interfaces;
using ModbusLib.Models;

namespace ModbusLib.Protocols;

/// <summary>
/// RTU协议处理器
/// </summary>
public class RtuProtocol : IModbusProtocol
{
    public byte[] BuildRequest(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var pdu = BuildPdu(request);
        var frame = new byte[pdu.Length + 3]; // SlaveId + PDU + CRC
        
        frame[0] = request.SlaveId;
        Array.Copy(pdu, 0, frame, 1, pdu.Length);
        
        // 计算并添加CRC
        var crc = ModbusUtils.CalculateCrc16(frame, 0, frame.Length - 2);
        frame[^2] = (byte)(crc & 0xFF);
        frame[^1] = (byte)(crc >> 8);
        
        return frame;
    }

    public ModbusResponse ParseResponse(byte[] response, ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));
        if (response.Length < 3)
            throw new ModbusCommunicationException($"RTU响应长度不足: {response.Length}");

        if (!ModbusUtils.ValidateCrc16(response))
            throw new ModbusCommunicationException("RTU响应CRC校验失败");

        var slaveId = response[0];
        var functionCode = response[1];
        
        // 检查是否为异常响应
        if ((functionCode & 0x80) != 0)
        {
            if (response.Length < 5)
                throw new ModbusCommunicationException("RTU异常响应长度不足");
                
            var originalFunction = (ModbusFunction)(functionCode & 0x7F);
            var exceptionCode = (ModbusExceptionCode)response[2];
            
            return ModbusResponse.CreateError(slaveId, originalFunction, exceptionCode);
        }

        // 解析正常响应
        var dataLength = response.Length - 3; // 减去SlaveId + CRC(2字节)
        var data = new byte[dataLength - 1]; // 减去功能码
        Array.Copy(response, 2, data, 0, data.Length);

        return new ModbusResponse(slaveId, (ModbusFunction)functionCode, data, response);
    }

    public bool ValidateResponse(byte[] response)
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));
        if (response.Length < 3)
            return false;
            
        return ModbusUtils.ValidateCrc16(response);
    }

    public int CalculateExpectedResponseLength(ModbusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        return request.Function switch
        {
            ModbusFunction.ReadCoils => 3 + 1 + (request.Quantity + 7) / 8 + 2, // SlaveId + Function + ByteCount + Data + CRC
            ModbusFunction.ReadDiscreteInputs => 3 + 1 + (request.Quantity + 7) / 8 + 2,
            ModbusFunction.ReadHoldingRegisters => 3 + 1 + request.Quantity * 2 + 2,
            ModbusFunction.ReadInputRegisters => 3 + 1 + request.Quantity * 2 + 2,
            ModbusFunction.WriteSingleCoil => 8, // Echo请求
            ModbusFunction.WriteSingleRegister => 8,
            ModbusFunction.WriteMultipleCoils => 8,
            ModbusFunction.WriteMultipleRegisters => 8,
            ModbusFunction.ReadWriteMultipleRegisters => 3 + 1 + request.Quantity * 2 + 2,
            _ => throw new NotSupportedException($"不支持的功能码: {request.Function}")
        };
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
        if (request.Data.IsEmpty || request.Data.Length < 1)
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
        if (request.Data.IsEmpty || request.Data.Length < 2)
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
        if (request.Data.IsEmpty)
            throw new ArgumentException("WriteMultipleCoils需要数据");

        var byteCount = (byte)request.Data.Length;
        var pdu = new byte[6 + byteCount];
        
        pdu[0] = (byte)request.Function;
        pdu[1] = (byte)(request.StartAddress >> 8);
        pdu[2] = (byte)(request.StartAddress & 0xFF);
        pdu[3] = (byte)(request.Quantity >> 8);
        pdu[4] = (byte)(request.Quantity & 0xFF);
        pdu[5] = byteCount;
        
        Array.Copy(request.Data.ToArray(), 0, pdu, 6, byteCount);
        
        return pdu;
    }

    private static byte[] BuildWriteMultipleRegistersPdu(ModbusRequest request)
    {
        if (request.Data.IsEmpty)
            throw new ArgumentException("WriteMultipleRegisters需要数据");

        var byteCount = (byte)request.Data.Length;
        var pdu = new byte[6 + byteCount];
        
        pdu[0] = (byte)request.Function;
        pdu[1] = (byte)(request.StartAddress >> 8);
        pdu[2] = (byte)(request.StartAddress & 0xFF);
        pdu[3] = (byte)(request.Quantity >> 8);
        pdu[4] = (byte)(request.Quantity & 0xFF);
        pdu[5] = byteCount;
        
        Array.Copy(request.Data.ToArray(), 0, pdu, 6, byteCount);
        
        return pdu;
    }

    private static byte[] BuildReadWriteMultipleRegistersPdu(ModbusRequest request)
    {
        if (request.Data.IsEmpty || request.Data.Length < 4)
            throw new ArgumentException("ReadWriteMultipleRegisters需要额外参数数据");

        // 数据格式: [写入起始地址2字节] + [写入数量2字节] + [写入数据...]
        var writeStartAddress = (ushort)((request.Data[0] << 8) | request.Data[1]);
        var writeQuantity = (ushort)((request.Data[2] << 8) | request.Data[3]);
        var writeData = new byte[request.Data.Length - 4];
        Array.Copy(request.Data.ToArray(), 4, writeData, 0, writeData.Length);

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