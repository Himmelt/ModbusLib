using ModbusLib.Enums;
using ModbusLib.Models;
using ModbusLib.Protocols;
using ModbusLib.Utils;

namespace ModbusLib.Tests.Functional;

public class RtuExceptionFrameTests {
    [Fact]
    public void Test_Parse_Rtu_Exception_Frame() {
        // Arrange
        var protocol = new RtuProtocol();
        var request = new ModbusRequest(1, ModbusFunction.ReadHoldingRegisters, 0, 10);

        // 模拟异常响应帧: 03 83 02 60 44
        // 03: Unit ID
        // 83: Function code (0x80 | 0x03 = 0x83) 表示异常响应，原始功能码是0x03
        // 02: Exception code (IllegalDataAddress)
        // 60 44: CRC (需要正确计算)
        var exceptionResponseWithoutCrc = new byte[] { 0x03, 0x83, 0x02 };
        var crc = Crc16Utils.CalculateCrc16(exceptionResponseWithoutCrc);
        var exceptionResponse = new byte[exceptionResponseWithoutCrc.Length + 2];
        Array.Copy(exceptionResponseWithoutCrc, exceptionResponse, exceptionResponseWithoutCrc.Length);
        exceptionResponse[exceptionResponse.Length - 2] = (byte)(crc & 0xFF);
        exceptionResponse[exceptionResponse.Length - 1] = (byte)(crc >> 8);

        // Act
        var response = protocol.ParseResponse(exceptionResponse, request);

        // Assert
        Assert.True(response.IsError);
        Assert.Equal((byte)0x03, response.UnitId);
        Assert.Equal(ModbusFunction.ReadHoldingRegisters, response.Function); // 应该还原为原始功能码
        Assert.Equal(ModbusExceptionCode.IllegalDataAddress, response.ExceptionCode);
    }

    [Fact]
    public void Test_Parse_Rtu_Normal_Frame() {
        // Arrange
        var protocol = new RtuProtocol();
        var request = new ModbusRequest(1, ModbusFunction.ReadHoldingRegisters, 0, 2);

        // 模拟正常响应帧: 01 03 04 00 0A 01 01 xx xx (xx xx是CRC)
        // 01: Unit ID
        // 03: Function code
        // 04: Byte count
        // 00 0A: Register 1 data (0x000A)
        // 01 01: Register 2 data (0x0101)
        // CRC: 自动计算

        // 先计算正确的CRC
        var normalResponseWithoutCrc = new byte[] { 0x01, 0x03, 0x04, 0x00, 0x0A, 0x01, 0x01 };
        var crc = Crc16Utils.CalculateCrc16(normalResponseWithoutCrc);
        var normalResponse = new byte[normalResponseWithoutCrc.Length + 2];
        Array.Copy(normalResponseWithoutCrc, normalResponse, normalResponseWithoutCrc.Length);
        normalResponse[normalResponse.Length - 2] = (byte)(crc & 0xFF);
        normalResponse[normalResponse.Length - 1] = (byte)(crc >> 8);

        // Act
        var response = protocol.ParseResponse(normalResponse, request);

        // Assert
        Assert.False(response.IsError);
        Assert.Equal((byte)0x01, response.UnitId);
        Assert.Equal(ModbusFunction.ReadHoldingRegisters, response.Function);
        Assert.Null(response.ExceptionCode);

        // 根据RTU协议解析逻辑:
        // response = [01 03 04 00 0A 01 01 xx xx] (共9字节)
        // response.Length = 9
        // dataLength = response.Length - 3 = 9 - 3 = 6 (减去设备地址 + CRC)
        // data = new byte[dataLength - 1] = new byte[5] (减去功能码)
        // Array.Copy(response, 2, data, 0, 5) 复制response[2]到response[6]，即04 00 0A 01 01
        Assert.Equal(5, response.Data.Length);
        Assert.Equal(0x04, response.Data[0]); // Byte count
        Assert.Equal(0x00, response.Data[1]); // Register 1 high byte
        Assert.Equal(0x0A, response.Data[2]); // Register 1 low byte
        Assert.Equal(0x01, response.Data[3]); // Register 2 high byte
        Assert.Equal(0x01, response.Data[4]); // Register 2 low byte
    }

    [Fact]
    public void Test_Debug_Rtu_Parsing() {
        // 用于调试RTU解析逻辑
        var response = new byte[] { 0x01, 0x03, 0x04, 0x00, 0x0A, 0x01, 0x01 };

        // 模拟RTU协议中的解析逻辑
        var dataLength = response.Length - 3; // 减去设备地址 + CRC(2字节)
        var data = new byte[dataLength - 1]; // 减去功能码
        Array.Copy(response, 2, data, 0, data.Length);

        // 验证计算结果
        Assert.Equal(4, dataLength);
        Assert.Equal(3, data.Length);
        Assert.Equal(0x04, data[0]);
        Assert.Equal(0x00, data[1]);
        Assert.Equal(0x0A, data[2]);
    }
}