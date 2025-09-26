# 功能性测试使用说明

## 概述

本目录包含了使用NModbus库模拟Modbus从机的功能性测试。这些测试验证了ModbusLib库与标准Modbus协议的兼容性，包括：

1. 线圈读写操作
2. 寄存器读写操作
3. 与第三方Modbus实现的互操作性

## 测试结构

### ModbusSlaveSimulatorTests.cs
主要测试类，包含以下测试方法：

1. `ModbusTcpSlave_ReadWriteCoils_Test` - 测试线圈读写功能
2. `ModbusTcpSlave_ReadWriteRegisters_Test` - 测试寄存器读写功能
3. `ModbusLibClient_ConnectToNModbusSlave_Test` - 测试我们自己的客户端与NModbus从机的兼容性

### 辅助类

1. `ModbusSlaveSimulator` - 使用NModbus创建TCP从机模拟器
2. `SlaveStorage` - 实现从机数据存储
3. `HoldingRegisters`, `InputRegisters`, `Coils`, `DiscreteInputs` - 各种数据点的实现

## 运行测试

### 运行所有测试
```bash
dotnet test
```

### 运行特定测试
```bash
dotnet test --filter "ModbusTcpSlave_ReadWriteCoils_Test"
```

## 扩展测试

### 添加新的功能测试

1. 在`ModbusSlaveSimulatorTests`类中添加新的测试方法
2. 使用`[Fact]`或`[Theory]`属性标记测试方法
3. 使用`ModbusSlaveSimulator`创建从机实例
4. 使用NModbus主站或我们自己的客户端进行测试

### 添加新的数据点类型

1. 创建新的类实现`IPointSource<T>`接口
2. 实现`ReadPoints`和`WritePoints`方法
3. 在`SlaveStorage`中添加新的属性

## 注意事项

1. 测试使用503端口，请确保该端口未被其他程序占用
2. 每个测试都会自动启动和停止从机模拟器
3. 测试完成后会自动清理资源