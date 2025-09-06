using FluentAssertions;
using ModbusLib.Enums;
using ModbusLib.Exceptions;
using ModbusLib.Models;
using ModbusLib.Protocols;
using Xunit;

namespace ModbusLib.Tests.Protocols;

/// <summary>
/// TCP协议测试
/// </summary>
public class TcpProtocolTests
{
    private readonly TcpProtocol _protocol;

    public TcpProtocolTests()
    {
        _protocol = new TcpProtocol();
    }

    #region BuildRequest Tests

    [Fact]
    public void BuildRequest_ReadCoils_CorrectMbapHeader()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().NotBeNull();
        frame.Should().HaveCount(12); // MBAP(6) + SlaveId(1) + PDU(5)
        
        // Check MBAP Header structure
        var transactionId = (ushort)((frame[0] << 8) | frame[1]);
        transactionId.Should().BeGreaterThan(0); // Should have transaction ID
        
        frame[2].Should().Be(0x00); // Protocol ID high byte
        frame[3].Should().Be(0x00); // Protocol ID low byte
        
        var length = (ushort)((frame[4] << 8) | frame[5]);
        length.Should().Be(6); // SlaveId(1) + PDU(5)
        
        frame[6].Should().Be(1); // SlaveId
    }

    [Fact]
    public void BuildRequest_TransactionIdIncrement_Sequential()
    {
        // Arrange
        var request1 = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);
        var request2 = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act
        var frame1 = _protocol.BuildRequest(request1);
        var frame2 = _protocol.BuildRequest(request2);

        // Assert
        var transactionId1 = (ushort)((frame1[0] << 8) | frame1[1]);
        var transactionId2 = (ushort)((frame2[0] << 8) | frame2[1]);
        
        transactionId2.Should().Be((ushort)(transactionId1 + 1));
    }

    [Fact]
    public void BuildRequest_ReadHoldingRegisters_CorrectFrame()
    {
        // Arrange
        var request = new ModbusRequest(5, ModbusFunction.ReadHoldingRegisters, 100, 10);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(12); // MBAP(6) + SlaveId(1) + PDU(5)
        frame[6].Should().Be(5); // SlaveId
        frame[7].Should().Be(0x03); // Function Code
        frame[8].Should().Be(0x00); // Start Address High
        frame[9].Should().Be(0x64); // Start Address Low (100)
        frame[10].Should().Be(0x00); // Quantity High
        frame[11].Should().Be(0x0A); // Quantity Low (10)
    }

    [Fact]
    public void BuildRequest_WriteSingleCoil_CorrectMbapLength()
    {
        // Arrange
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleCoil, 50, 1, new byte[] { 1 });

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(12); // MBAP(6) + SlaveId(1) + PDU(5)
        
        var length = (ushort)((frame[4] << 8) | frame[5]);
        length.Should().Be(6); // SlaveId(1) + PDU(5)
        
        // Check coil value encoding
        frame[10].Should().Be(0xFF); // Value High (0xFF00 for true)
        frame[11].Should().Be(0x00); // Value Low
    }

    [Fact]
    public void BuildRequest_WriteMultipleRegisters_CorrectLength()
    {
        // Arrange
        var values = new ushort[] { 100, 200, 300 };
        var data = ModbusUtils.UshortArrayToByteArray(values);
        var request = new ModbusRequest(1, ModbusFunction.WriteMultipleRegisters, 0, (ushort)values.Length, data);

        // Act
        var frame = _protocol.BuildRequest(request);

        // Assert
        frame.Should().HaveCount(19); // MBAP(6) + SlaveId(1) + PDU(12: Function + Address + Quantity + ByteCount + Data)
        
        var length = (ushort)((frame[4] << 8) | frame[5]);
        length.Should().Be(13); // SlaveId(1) + PDU(12)
        
        frame[7].Should().Be(0x10); // Function Code
        frame[12].Should().Be(6); // Byte count (3 registers * 2 bytes)
    }

    [Fact]
    public void BuildRequest_TransactionIdWrapAround_HandlesOverflow()
    {
        // Arrange - Create a new protocol to test wrap-around
        var protocol = new TcpProtocol();
        
        // Force transaction ID close to max value through reflection
        var field = typeof(TcpProtocol).GetField("_transactionId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(protocol, (ushort)65534);

        var request1 = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);
        var request2 = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);
        var request3 = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act
        var frame1 = protocol.BuildRequest(request1);
        var frame2 = protocol.BuildRequest(request2);
        var frame3 = protocol.BuildRequest(request3);

        // Assert
        var transactionId1 = (ushort)((frame1[0] << 8) | frame1[1]);
        var transactionId2 = (ushort)((frame2[0] << 8) | frame2[1]);
        var transactionId3 = (ushort)((frame3[0] << 8) | frame3[1]);
        
        transactionId1.Should().Be(65535);
        transactionId2.Should().Be(0); // Wrapped around
        transactionId3.Should().Be(1);
    }

    #endregion

    #region ParseResponse Tests

    [Fact]
    public void ParseResponse_ReadCoilsValid_CorrectParsing()
    {
        // Arrange
        var responseData = new byte[] 
        { 
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x04, // Length (4 bytes: SlaveId + Function + ByteCount + Data)
            0x01,       // SlaveId
            0x01,       // Function Code
            0x01,       // Byte Count
            0xFF        // Data
        };
        
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
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x07, // Length (7 bytes)
            0x01,       // SlaveId
            0x03,       // Function Code
            0x04,       // Byte Count
            0x01, 0x00, // Register 1 value
            0x02, 0x00  // Register 2 value
        };
        
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
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x03, // Length (3 bytes: SlaveId + Function + Exception)
            0x01,       // SlaveId
            0x83,       // Function Code with error bit (0x03 | 0x80)
            0x02        // Exception Code
        };
        
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
    public void ParseResponse_InvalidProtocolId_ThrowsException()
    {
        // Arrange - Response with invalid Protocol ID
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x01, // Invalid Protocol ID (should be 0x0000)
            0x00, 0x04, // Length
            0x01,       // SlaveId
            0x01,       // Function Code
            0x01,       // Byte Count
            0xFF        // Data
        };

        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act & Assert
        Assert.Throws<ModbusCommunicationException>(() => 
            _protocol.ParseResponse(responseData, request));
    }

    [Fact]
    public void ParseResponse_IncompleteData_ThrowsException()
    {
        // Arrange - Response with incomplete data
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x05, // Length (says 5 bytes but only has 3)
            0x01,       // SlaveId
            0x01,       // Function Code
            0x01        // Byte Count (missing data)
        };

        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act & Assert
        Assert.Throws<ModbusCommunicationException>(() => 
            _protocol.ParseResponse(responseData, request));
    }

    [Fact]
    public void ParseResponse_TooShort_ThrowsException()
    {
        // Arrange - Response too short
        var responseData = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00 };

        var request = new ModbusRequest(1, ModbusFunction.ReadCoils, 0, 8);

        // Act & Assert
        Assert.Throws<ModbusCommunicationException>(() => 
            _protocol.ParseResponse(responseData, request));
    }

    #endregion

    #region ValidateResponse Tests

    [Fact]
    public void ValidateResponse_ValidResponse_ReturnsTrue()
    {
        // Arrange
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x04, // Length
            0x01,       // SlaveId
            0x01,       // Function Code
            0x01,       // Byte Count
            0xFF        // Data
        };

        // Act
        var isValid = _protocol.ValidateResponse(responseData);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateResponse_InvalidProtocolId_ReturnsFalse()
    {
        // Arrange
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x01, // Invalid Protocol ID
            0x00, 0x04, // Length
            0x01,       // SlaveId
            0x01,       // Function Code
            0x01,       // Byte Count
            0xFF        // Data
        };

        // Act
        var isValid = _protocol.ValidateResponse(responseData);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateResponse_IncorrectLength_ReturnsFalse()
    {
        // Arrange
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x10, // Length (says 16 bytes but total is only 8)
            0x01,       // SlaveId
            0x01        // Function Code
        };

        // Act
        var isValid = _protocol.ValidateResponse(responseData);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateResponse_TooShort_ReturnsFalse()
    {
        // Arrange
        var responseData = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00 };

        // Act
        var isValid = _protocol.ValidateResponse(responseData);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region CalculateExpectedResponseLength Tests

    [Theory]
    [InlineData(ModbusFunction.ReadCoils, 8, 10)] // 6 + 1 + 2 + 1 = 10
    [InlineData(ModbusFunction.ReadCoils, 16, 11)] // 6 + 1 + 2 + 2 = 11
    [InlineData(ModbusFunction.ReadHoldingRegisters, 10, 29)] // 6 + 1 + 2 + 20 = 29
    [InlineData(ModbusFunction.ReadInputRegisters, 5, 19)] // 6 + 1 + 2 + 10 = 19
    [InlineData(ModbusFunction.WriteSingleCoil, 1, 12)] // 6 + 1 + 5 = 12
    [InlineData(ModbusFunction.WriteSingleRegister, 1, 12)]
    [InlineData(ModbusFunction.WriteMultipleCoils, 16, 12)]
    [InlineData(ModbusFunction.WriteMultipleRegisters, 10, 12)]
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
    public void BuildRequest_UnsupportedFunction_ThrowsNotSupportedException()
    {
        // Arrange
        var request = new ModbusRequest(1, (ModbusFunction)0xFF, 0, 1);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _protocol.BuildRequest(request));
    }

    [Fact]
    public void ParseResponse_EmptyDataResponse_HandlesCorrectly()
    {
        // Arrange - Response with no data (like write operations)
        var responseData = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length (6 bytes: SlaveId + Function + Address + Value)
            0x01,       // SlaveId
            0x05,       // Function Code (Write Single Coil)
            0x00, 0x32, // Address
            0xFF, 0x00  // Value
        };
        
        var request = new ModbusRequest(1, ModbusFunction.WriteSingleCoil, 50, 1);

        // Act
        var response = _protocol.ParseResponse(responseData, request);

        // Assert
        response.Should().NotBeNull();
        response.IsError.Should().BeFalse();
        response.SlaveId.Should().Be(1);
        response.Function.Should().Be(ModbusFunction.WriteSingleCoil);
        response.Data.Should().HaveCount(4); // Address + Value
    }

    #endregion
}