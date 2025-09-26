# 功能性测试说明

本目录包含使用NModbus库模拟Modbus从机的功能性测试。这些测试验证了ModbusLib库与标准Modbus协议的兼容性。

## 测试内容

1. **ModbusSlaveSimulatorTests.cs** - 使用NModbus创建TCP从机模拟器，并通过以下方式测试：
   - 使用NModbus主站连接到从机进行读写操作
   - 使用我们自己的Modbus客户端连接到NModbus从机进行读写操作

## 测试场景

测试覆盖以下Modbus功能：
- 线圈读写 (功能码 01, 05)
- 寄存器读写 (功能码 03, 06, 16)
- 数据存储和检索验证

## 运行测试

```bash
dotnet test
```

## 实现细节

### ModbusSlaveSimulator类
- 使用NModbus库创建TCP从机
- 实现了完整的数据存储（线圈、离散输入、保持寄存器、输入寄存器）
- 支持标准Modbus功能码操作

### 测试用例
1. `ModbusTcpSlave_ReadWriteCoils_Test` - 测试线圈读写功能
2. `ModbusTcpSlave_ReadWriteRegisters_Test` - 测试寄存器读写功能
3. `ModbusLibClient_ConnectToNModbusSlave_Test` - 测试我们自己的客户端与NModbus从机的兼容性

## 依赖

- NModbus 3.0.81
- xUnit
- Moq