# ModbusLib - .NET 9 Modbus Client Library

一个功能完整、高性能的Modbus客户端库，基于.NET 9平台，支持多种Modbus协议变体。

> **注意**：本项目是由AI生成的，主要使用了Trae以及Qoder等工具辅助开发，主要模型是 Qwen-3-Coder。

## 支持的协议

- **Modbus RTU** - 通过串口通信的RTU协议
- **Modbus TCP** - 基于TCP/IP的标准Modbus协议  
- **Modbus UDP** - 基于UDP的Modbus协议
- **Modbus RTU over TCP** - 通过TCP传输的RTU协议
- **Modbus RTU over UDP** - 通过UDP传输的RTU协议

## 功能特性

- 异步编程模型，提供卓越的性能
- 支持所有标准Modbus功能码
- 灵活的配置选项
- 完整的错误处理和重试机制
- 内存优化，使用 ArrayPool 减少分配
- 工厂模式，便于创建不同类型的客户端

## 快速开始

### 安装

```bash
dotnet add package ModbusLib
```

### 基本用法

```csharp
// 创建RTU客户端
var rtuClient = ModbusClientFactory.CreateRtuClient(new SerialConnectionConfig
{
    PortName = "COM1",
    BaudRate = 9600,
    Parity = Parity.None,
    DataBits = 8,
    StopBits = StopBits.One
});

// 连接并读取数据
await rtuClient.ConnectAsync();
var coils = await rtuClient.ReadCoilsAsync(slaveId: 1, startAddress: 0, quantity: 10);
```

```csharp
// 创建TCP客户端
var tcpClient = ModbusClientFactory.CreateTcpClient(new NetworkConnectionConfig
{
    Host = "192.168.1.100",
    Port = 502
});

// 连接并写入数据
await tcpClient.ConnectAsync();
await tcpClient.WriteMultipleRegistersAsync(slaveId: 1, startAddress: 0, new ushort[] { 100, 200, 300 });
```

## 项目结构

```
ModbusLib/
├── src/
│   ├── Interfaces/          # 核心接口定义
│   ├── Enums/              # 枚举类型
│   ├── Models/             # 数据模型
│   ├── Transports/         # 传输层实现
│   ├── Protocols/          # 协议处理层
│   ├── Clients/            # 客户端实现
│   ├── Factories/          # 工厂模式
│   └── Exceptions/         # 异常定义
└── ModbusLib.Tests/        # 单元测试
```

## 构建和测试

```bash
# 构建项目
dotnet build

# 运行测试
dotnet test

# 发布 NuGet 包
dotnet pack
```

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

本项目采用 MIT 许可证。