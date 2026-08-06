using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Protocols;

namespace ModbusLib.Tests.Protocols;

public class TcpProtocolTests {

    [Fact]
    public void ParseResponse_WithTinyMbapLength_ThrowsCommunicationExceptionInsteadOfOverflow() {
        var protocol = new TcpProtocol();
        var request = new ModbusRequest(1, ModbusFunction.ReadHoldingRegisters, 0, 1);
        var requestBytes = protocol.BuildRequest(request);

        // 构造 length=1 的畸形响应（MBAP 声明 1 字节，实际携带 3 字节 PDU 内容）
        var response = new byte[] {
            requestBytes[0], requestBytes[1],
            0x00, 0x00,
            0x00, 0x01,
            0x01, 0x03, 0x00
        };

        Assert.Throws<ModbusCommunicationException>(() => protocol.ParseResponse(response, request));
    }

    [Fact]
    public void ParseResponse_WithMismatchedTransactionId_Throws() {
        var protocol = new TcpProtocol();
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleRegister, 0, 1, [0x12, 0x34]);
        protocol.BuildRequest(request); // 生成事务ID

        // 响应携带不同的事务ID
        var response = new byte[] { 0x00, 0x02, 0x00, 0x00, 0x00, 0x06, 0x01, 0x06, 0x00, 0x00, 0x12, 0x34 };

        Assert.Throws<ModbusCommunicationException>(() => protocol.ParseResponse(response, request));
    }

    [Fact]
    public void ParseResponse_WithMatchingTransactionId_ReturnsResponse() {
        var protocol = new TcpProtocol();
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleRegister, 0, 1, [0x12, 0x34]);
        var requestBytes = protocol.BuildRequest(request);

        var response = new byte[] {
            requestBytes[0], requestBytes[1],
            0x00, 0x00,
            0x00, 0x06,
            0x01, 0x06,
            0x00, 0x00,
            0x12, 0x34
        };

        var parsed = protocol.ParseResponse(response, request);
        Assert.False(parsed.IsError);
        // 写单个寄存器响应回显 地址(2) + 值(2)
        Assert.Equal(4, parsed.Data.Length);
        Assert.Equal(0x1234, (parsed.Data[2] << 8) | parsed.Data[3]);
    }

    [Fact]
    public void BuildRequest_WriteMultipleRegistersWithOddData_Throws() {
        var protocol = new TcpProtocol();
        var request = new ModbusRequest(1, ModbusFunction.WriteMultipleRegisters, 0, 1, [0x12, 0x34, 0x56]);

        Assert.Throws<ArgumentException>(() => protocol.BuildRequest(request));
    }

    [Fact]
    public void BuildRequest_WriteMultipleCoilsWithWrongPackedLength_Throws() {
        var protocol = new TcpProtocol();
        var request = new ModbusRequest(1, ModbusFunction.WriteMultipleCoils, 0, 8, [0x01, 0x02]);

        Assert.Throws<ArgumentException>(() => protocol.BuildRequest(request));
    }
}
