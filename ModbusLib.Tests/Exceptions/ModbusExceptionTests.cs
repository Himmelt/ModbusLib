using ModbusLib.Enums;
using ModbusLib.Exceptions;

namespace ModbusLib.Tests.Exceptions;

public class ModbusExceptionTests
{
    [Fact]
    public void ModbusException_ConstructorWithExceptionCodeUnitIdFunctionAndMessage_SetsProperties()
    {
        // Arrange
        var exceptionCode = ModbusExceptionCode.IllegalDataAddress;
        var unitId = (byte)2;
        var function = ModbusFunction.WriteSingleCoil;
        var message = "Test message";

        // Act
        var exception = new ModbusException(exceptionCode, unitId, function, message);

        // Assert
        Assert.Equal(exceptionCode, exception.ExceptionCode);
        Assert.Equal(unitId, exception.UnitId);
        Assert.Equal(function, exception.Function);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ModbusException_ConstructorWithExceptionCodeUnitIdFunctionMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var exceptionCode = ModbusExceptionCode.TargetDeviceFailure;
        var unitId = (byte)3;
        var function = ModbusFunction.ReadHoldingRegisters;
        var message = "Test message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new ModbusException(exceptionCode, unitId, function, message, innerException);

        // Assert
        Assert.Equal(exceptionCode, exception.ExceptionCode);
        Assert.Equal(unitId, exception.UnitId);
        Assert.Equal(function, exception.Function);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ModbusException_DefaultConstructor_CreatesException()
    {
        // Act
        var exception = new ModbusException();

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("Exception of type 'ModbusLib.Exceptions.ModbusException' was thrown.", exception.Message);
    }

    [Fact]
    public void ModbusException_ConstructorWithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new ModbusException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ModbusException_ConstructorWithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "Test message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new ModbusException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}