# FluentModbus 从机功能测试说明

## 概述

本测试套件使用 [FluentModbus](https://github.com/Apollo3zehn/FluentModbus) 库作为Modbus从机，对ModbusLib客户端进行功能测试。FluentModbus是一个轻量级且快速的Modbus协议实现，支持TCP/RTU协议以及同步/异步操作。

## 测试内容

### 1. 线圈读写测试 (ReadWriteCoilsTest)
- 测试单个线圈的写入和读取
- 测试多个线圈的批量写入和读取
- 验证写入和读取的数据一致性

### 2. 寄存器读写测试 (ReadWriteRegistersTest)
- 测试单个寄存器的写入和读取
- 测试多个寄存器的批量写入和读取
- 验证写入和读取的数据一致性

### 3. 泛型寄存器读写测试 (ReadWriteGenericRegistersTest)
- 测试浮点数数组的读写操作
- 测试双精度浮点数的读写操作
- 验证不同类型数据的正确转换

### 4. 读写多个寄存器测试 (ReadWriteMultipleRegistersTest)
- 测试同时读取和写入寄存器的操作
- 验证原子性操作的正确性

## 测试环境

- **从机**: FluentModbus TCP服务器，运行在 localhost:504
- **客户端**: ModbusLib TCP客户端
- **协议**: Modbus TCP

## 运行测试

```bash
dotnet test --filter "FullyQualifiedName~FluentModbusSlaveTests"
```

## 依赖项

- FluentModbus库作为测试从机
- ModbusLib作为测试客户端
- xUnit作为测试框架

## 注意事项

1. 测试使用端口504，确保该端口未被其他程序占用
2. 每个测试方法都会自动启动和停止FluentModbus服务器
3. 测试完成后会自动清理资源