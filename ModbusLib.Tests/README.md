# ModbusLib 测试项目结构

## 目录结构

- **Clients**: 客户端相关测试
  - ModbusClientBaseTests.cs
  - ModbusClientUsageTests.cs
  - ModbusTcpClientTests.cs

- **Enums**: 枚举类型测试
  - ModbusConnectionTypeTests.cs
  - ModbusExceptionCodeTests.cs
  - ModbusFunctionTests.cs

- **Exceptions**: 异常类测试
  - ModbusExceptionTests.cs

- **Factories**: 工厂类测试
  - ModbusClientFactoryTests.cs

- **Functional**: 功能性集成测试
  - ModbusSlaveSimulatorTests.cs (使用NModbus模拟从机进行功能测试)

- **Models**: 模型类测试
  - ModbusDataConverterTests.cs

- **Protocols**: 协议相关测试
  - ModbusUtilsTests.cs

- **Transports**: 传输层测试
  - SerialTransportTests.cs
  - TcpTransportTests.cs
  - UdpTransportTests.cs

## 运行测试

```bash
dotnet test
```