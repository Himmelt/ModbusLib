using FluentAssertions;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Protocols;
using Xunit;

namespace ModbusLib.Tests.Protocols;

/// <summary>
/// RTU协议测试
/// </summary>
public class RtuProtocolTests
{
    private readonly RtuProtocol _protocol;

    public RtuProtocolTests()
    {
        _protocol = new RtuProtocol();
    }

    #region BuildRequest Tests

    [Fact]
    public void BuildRequest_ReadCoils_CorrectFrame()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().NotBeNull();
        frame.Should().HaveCount(8); // SlaveId + Function + Address + Quantity + CRC
        frame[0].Should().Be(1); // SlaveId
        frame[1].Should().Be(0x01); // Function Code
        frame[2].Should().Be(0x00); // Start Address High
        frame[3].Should().Be(0x00); // Start Address Low
        frame[4].Should().Be(0x00); // Quantity High
        frame[5].Should().Be(0x08); // Quantity Low
        // Last 2 bytes are CRC
    }

    [Fact]
    public void BuildRequest_ReadHoldingRegisters_CorrectFrame()
    {
        // Arrange
        var request = new ModbusRequest(5, ModbusFunction.ReadHoldingRegisters, 100, 10);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(8);
        frame[0].Should().Be(5); // SlaveId
        frame[1].Should().Be(0x03); // Function Code
        frame[2].Should().Be(0x00); // Start Address High
        frame[3].Should().Be(0x64); // Start Address Low (100)
        frame[4].Should().Be(0x00); // Quantity High
        frame[5].Should().Be(0x0A); // Quantity Low (10)
    }

    [Fact]
    public void BuildRequest_WriteSingleCoil_TrueValue()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleCoil, 50, 1, new byte[] { 1 });

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(8);
        frame[0].Should().Be(1); // SlaveId
        frame[1].Should().Be(0x05); // Function Code
        frame[2].Should().Be(0x00); // Address High
        frame[3].Should().Be(0x32); // Address Low (50)
        frame[4].Should().Be(0xFF); // Value High (0xFF00 for true)
        frame[5].Should().Be(0x00); // Value Low
    }

    [Fact]
    public void BuildRequest_WriteSingleCoil_FalseValue()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleCoil, 50, 1, new byte[] { 0 });

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame[4].Should().Be(0x00); // Value High (0x0000 for false)
        frame[5].Should().Be(0x00); // Value Low
    }

    [Fact]
    public void BuildRequest_WriteSingleRegister_CorrectValue()
    {
        // Arrange
        var value = (ushort)0x1234;
        var data = new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleRegister, 100, 1, data);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(8);
        frame[1].Should().Be(0x06); // Function Code
        frame[4].Should().Be(0x12); // Value High
        frame[5].Should().Be(0x34); // Value Low
    }

    [Fact]
    public void BuildRequest_WriteMultipleCoils_CorrectFrame()
    {
        // Arrange
        var coilData = new bool[] { true, false, true, false, true, false, true, true };
        var data = ModbusUtils.BoolArrayToByteArray(coilData);
        var request = new ModbusRequest(1, ModbusFunction.WriteMultipleCoils, 0, (ushort)coilData.Length, data);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(10); // SlaveId + Function + Address + Quantity + ByteCount + Data + CRC
        frame[1].Should().Be(0x0F); // Function Code
        frame[6].Should().Be((byte)data.Length); // Byte count
        frame[7].Should().Be(data[0]); // First data byte
    }

    [Fact]
    public void BuildRequest_WriteMultipleRegisters_CorrectFrame()
    {
        // Arrange
        var values = new ushort[] { 100, 200, 300 };
        var data = ModbusUtils.UshortArrayToByteArray(values);
        var request = new ModbusRequest(1, ModbusFunction.WriteMultipleRegisters, 0, (ushort)values.Length, data);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(15); // SlaveId + Function + Address + Quantity + ByteCount + Data(6) + CRC
        frame[1].Should().Be(0x10); // Function Code
        frame[6].Should().Be(6); // Byte count (3 registers * 2 bytes)
    }

    [Fact]
    public void BuildRequest_CrcCalculation_ValidChecksum()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        var crcCalculated = ModbusUtils.CalculateCrc16(frame, 0, frame.Length - 2);
        var crcInFrame = (ushort)(frame[frame.Length - 2] | (frame[frame.Length - 1] << 8));
        crcInFrame.Should().Be(crcCalculated);
    }

    #endregion

    #region ParseResponse Tests

    [Fact]
    public void ParseResponse_ReadCoilsValid_CorrectParsing()
    {
        // Arrange
        var responseData = new byte[] { 0x01, 0x01, 0x01, 0xFF, 0x00, 0x00 }; // CRC will be calculated
        var crc = ModbusUtils.CalculateCrc16(responseData, 0, 4);
        responseData[4] = (byte)(crc & 0xFF);
        responseData[5] = (byte)(crc >> 8);
        
        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act
        var response = _protocol.ParseResponse(responseData, request);

        // Assert
        response.Should().NotBeNull();
        response.IsError.Should().BeFalse();
        response.SlaveId.Should().Be(1);
        response.Function.Should().Be(ModbusFunction.ReadCoils);
        response.Data.Should().NotBeNull();
        response.Data.Should().HaveCount(2); // Byte count + data
        response.Data![0].Should().Be(0x01); // Byte count
        response.Data[1].Should().Be(0xFF); // Data
    }

    [Fact]
    public void ParseResponse_ReadHoldingRegistersValid_CorrectParsing()
    {
        // Arrange
        var responseData = new byte[] { 0x01, 0x03, 0x04, 0x01, 0x00, 0x02, 0x00, 0x00, 0x00 }; // CRC placeholder
        var crc = ModbusUtils.CalculateCrc16(responseData, 0, 7);
        responseData[7] = (byte)(crc & 0xFF);
        responseData[8] = (byte)(crc >> 8);
        
        var request = new ModbusRequest(1, ModbusFunction.ReadHoldingRegisters, 0, 2);

        // Act
        var response = _protocol.ParseResponse(responseData, request);

        // Assert
        response.Should().NotBeNull();
        response.IsError.Should().BeFalse();
        response.SlaveId.Should().Be(1);
        response.Function.Should().Be(ModbusFunction.ReadHoldingRegisters);
        response.Data.Should().HaveCount(5); // Byte count + 4 data bytes
        response.Data![0].Should().Be(0x04); // Byte count
    }

    [Fact]
    public void ParseResponse_ErrorResponse_CorrectErrorParsing()
    {
        // Arrange - Error response with exception code 02 (Illegal Data Address)
        var responseData = new byte[] { 0x01, 0x83, 0x02, 0x00, 0x00 }; // CRC placeholder
        var crc = ModbusUtils.CalculateCrc16(responseData, 0, 3);
        responseData[3] = (byte)(crc & 0xFF);
        responseData[4] = (byte)(crc >> 8);
        
        var request = new ModbusRequest(1, ModbusFunction.ReadHoldingRegisters, 0, 1);

        // Act
        var response = _protocol.ParseResponse(responseData, request);

        // Assert
        response.Should().NotBeNull();
        response.IsError.Should().BeTrue();
        response.SlaveId.Should().Be(1);
        response.Function.Should().Be(ModbusFunction.ReadHoldingRegisters);
        response.ExceptionCode.Should().Be(ModbusExceptionCode.IllegalDataAddress);
    }

    [Fact]
    public void ParseResponse_InvalidCrc_ThrowsException()
    {
        // Arrange - Response with invalid CRC
        var responseData = new byte[] { 0x01, 0x01, 0x01, 0xFF, 0xFF, 0xFF }; // Wrong CRC

        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act & Assert
        Assert.Throws<ModbusCommunicationException>(() => 
            _protocol.ParseResponse(responseData, request));
    }

    [Fact]
    public void ParseResponse_TooShort_ThrowsException()
    {
        // Arrange - Response too short
        var responseData = new byte[] { 0x01, 0x01 };

        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act & Assert
        Assert.Throws<ModbusCommunicationException>(() => 
            _protocol.ParseResponse(responseData, request));
    }

    #endregion

    #region ValidateResponse Tests

    [Fact]
    public void ValidateResponse_ValidCrc_ReturnsTrue()
    {
        // Arrange
        var responseData = new byte[] { 0x01, 0x01, 0x01, 0xFF, 0x00, 0x00 };
        var crc = ModbusUtils.CalculateCrc16(responseData, 0, 4);
        responseData[4] = (byte)(crc & 0xFF);
        responseData[5] = (byte)(crc >> 8);

        // Act
        var isValid = _protocol.ValidateResponse(responseData);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateResponse_InvalidCrc_ReturnsFalse()
    {
        // Arrange
        var responseData = new byte[] { 0x01, 0x01, 0x01, 0xFF, 0xFF, 0xFF }; // Wrong CRC

        // Act
        var isValid = _protocol.ValidateResponse(responseData);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateResponse_TooShort_ReturnsFalse()
    {
        // Arrange
        var responseData = new byte[] { 0x01, 0x01 };

        // Act
        var isValid = _protocol.ValidateResponse(responseData);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region CalculateExpectedResponseLength Tests

    [Theory]
    [InlineData(ModbusFunction.ReadCoils, 8, 7)] // 3 + 1 + 1 + 2 = 7 for 8 coils
    [InlineData(ModbusFunction.ReadCoils, 16, 8)] // 3 + 1 + 2 + 2 = 8 for 16 coils
    [InlineData(ModbusFunction.ReadHoldingRegisters, 10, 26)] // 3 + 1 + 20 + 2 = 26
    [InlineData(ModbusFunction.ReadInputRegisters, 5, 16)] // 3 + 1 + 10 + 2 = 16
    [InlineData(ModbusFunction.WriteSingleCoil, 1, 8)]
    [InlineData(ModbusFunction.WriteSingleRegister, 1, 8)]
    [InlineData(ModbusFunction.WriteMultipleCoils, 16, 8)]
    [InlineData(ModbusFunction.WriteMultipleRegisters, 10, 8)]
    public void CalculateExpectedResponseLength_VariousFunctions_CorrectLength(ModbusFunction function, ushort quantity, int expectedLength)
    {
        // Arrange
        var request = new ModbusRequest(1, function, 0, quantity);

        // Act
        var length = _protocol.CalculateExpectedResponseLength(request);

        // Assert
        length.Should().Be(expectedLength);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void BuildRequest_WriteSingleCoilNoData_ThrowsArgumentException()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleCoil, 0, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _protocol.BuildRequest(request));
    }

    [Fact]
    public void BuildRequest_WriteSingleRegisterInsufficientData_ThrowsArgumentException()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleRegister, 0, 1, new byte[] { 0x12 });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _protocol.BuildRequest(request));
    }

    [Fact]
    public void BuildRequest_WriteMultipleCoilsNoData_ThrowsArgumentException()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.WriteMultipleCoils, 0, 8);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _protocol.BuildRequest(request));
    }

    [Fact]
    public void BuildRequest_UnsupportedFunction_ThrowsNotSupportedException()
    {
        // Arrange
        var request = new ModbusRequest(1, (ModbusFunction)0xFF, 0, 1);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _protocol.BuildRequest(request));
    }

    #endregion
}