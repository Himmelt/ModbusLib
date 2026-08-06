using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Protocols;

namespace ModbusLib.Tests.Protocols;

public class RtuProtocolTests {

    [Fact]
    public void ParseResponse_WithThreeByteFrame_ThrowsCommunicationExceptionInsteadOfOverflow() {
        var protocol = new RtuProtocol();
        var request = new ModbusRequest(1, ModbusFunction.ReadHoldingRegisters, 0, 1);

        // 3 字节伪帧：设备地址 + 功能码 + 1 字节（无 CRC）
        var response = new byte[] { 0x01, 0x03, 0x00 };

        Assert.Throws<ModbusCommunicationException>(() => protocol.ParseResponse(response, request));
    }

    [Fact]
    public void ValidateResponse_WithShortFrame_ReturnsFalse() {
        var protocol = new RtuProtocol();

        Assert.False(protocol.ValidateResponse(new byte[] { 0x01, 0x03, 0x00 }));
    }

    [Fact]
    public void CalculateExpectedResponseLength_ReadCoils_IsExact() {
        var protocol = new RtuProtocol();
        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 10);

        // 设备地址1 + 功能码1 + 字节数1 + 数据2 + CRC2 = 7
        Assert.Equal(7, protocol.CalculateExpectedResponseLength(request));
    }

    [Fact]
    public void BuildRequest_WriteMultipleRegistersWithOddData_Throws() {
        var protocol = new RtuProtocol();
        var request = new ModbusRequest(1, ModbusFunction.WriteMultipleRegisters, 0, 1, [0x12, 0x34, 0x56]);

        Assert.Throws<ArgumentException>(() => protocol.BuildRequest(request));
    }
}
