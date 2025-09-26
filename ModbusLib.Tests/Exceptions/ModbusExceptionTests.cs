using ModbusLib.Enums;
using ModbusLib.Exceptions;

namespace ModbusLib.Tests.Exceptions;

public class ModbusExceptionTests
{
    [Fact]
    public void ModbusException_ConstructorWithExceptionCodeSlaveIdAndFunction_SetsProperties()
    {
        // Arrange
        var exceptionCode = ModbusExceptionCode.IllegalFunction;
        var slaveId = (byte)1;
        var function = ModbusFunction.ReadCoils;

        // Act
        var exception = new ModbusException(exceptionCode, slaveId, function);

        // Assert
        Assert.Equal(exceptionCode, exception.ExceptionCode);
        Assert.Equal(slaveId, exception.SlaveId);
        Assert.Equal(function, exception.Function);
        Assert.Equal($"Modbus异常: 从站{slaveId}, 功能码{(byte)function:X2}, 异常码{(byte)exceptionCode}", exception.Message);
    }

    [Fact]
    public void ModbusException_ConstructorWithExceptionCodeSlaveIdFunctionAndMessage_SetsProperties()
    {
        // Arrange
        var exceptionCode = ModbusExceptionCode.IllegalDataAddress;
        var slaveId = (byte)2;
        var function = ModbusFunction.WriteSingleCoil;
        var message = "Test message";

        // Act
        var exception = new ModbusException(exceptionCode, slaveId, function, message);

        // Assert
        Assert.Equal(exceptionCode, exception.ExceptionCode);
        Assert.Equal(slaveId, exception.SlaveId);
        Assert.Equal(function, exception.Function);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ModbusException_ConstructorWithExceptionCodeSlaveIdFunctionMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var exceptionCode = ModbusExceptionCode.SlaveDeviceFailure;
        var slaveId = (byte)3;
        var function = ModbusFunction.ReadHoldingRegisters;
        var message = "Test message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new ModbusException(exceptionCode, slaveId, function, message, innerException);

        // Assert
        Assert.Equal(exceptionCode, exception.ExceptionCode);
        Assert.Equal(slaveId, exception.SlaveId);
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
        Assert.Equal(string.Empty, exception.Message);
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