# ModbusLib - .NET 9 Modbus Client Library

一个功能完整、高性能的Modbus客户端库，基于.NET 9平台，支持多种Modbus协议变体。

> **注意**：本项目是由AI生成的，主要使用了Trae以及Qoder等工具辅助开发，主要模型是 Qwen-3-Coder。

## 📦 安装

```bash
dotnet add package Himmelt.ModbusLib
```

## 🌟 支持的协议

- **Modbus RTU** - 通过串口通信的RTU协议
- **Modbus TCP** - 基于TCP/IP的标准Modbus协议  
- **Modbus UDP** - 基于UDP的Modbus协议
- **Modbus RTU over TCP** - 通过TCP传输的RTU协议
- **Modbus RTU over UDP** - 通过UDP传输的RTU协议

## ✨ 功能特性

- 🚀 **异步编程模型**，提供卓越的性能
- 🔄 **同步方法支持**，便于传统应用集成
- 📋 **支持所有标准Modbus功能码**
- ⚙️ **灵活的配置选项**
- 🛡️ **完整的错误处理和重试机制**
- 💾 **内存优化**，使用 ArrayPool 减少分配
- 🏭 **工厂模式**，便于创建不同类型的客户端
- 🔀 **泛型支持**，可直接读写基本数据类型
- 📊 **字节序和字序控制**，支持不同平台数据格式

## 🚀 快速开始

### 基本用法

#### 异步方法使用示例

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
var coils = await rtuClient.ReadCoilsAsync(unitId: 1, startAddress: 0, quantity: 10);
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
await tcpClient.WriteMultipleRegistersAsync(unitId: 1, startAddress: 0, new ushort[] { 100, 200, 300 });
```

#### 同步方法使用示例

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
rtuClient.Connect();
var coils = rtuClient.ReadCoils(unitId: 1, startAddress: 0, quantity: 10);
```

```csharp
// 创建TCP客户端
var tcpClient = ModbusClientFactory.CreateTcpClient(new NetworkConnectionConfig
{
    Host = "192.168.1.100",
    Port = 502
});

// 连接并写入数据
tcpClient.Connect();
tcpClient.WriteMultipleRegisters(unitId: 1, startAddress: 0, new ushort[] { 100, 200, 300 });
```

### 泛型方法使用示例

```csharp
// 读取浮点数数组
var floatValues = await client.ReadHoldingRegistersAsync<float>(unitId: 1, startAddress: 0, count: 5);

// 读取双精度浮点数
var doubleValue = await client.ReadHoldingRegistersAsync<double>(unitId: 1, startAddress: 10, count: 1);

// 写入浮点数数组
float[] valuesToWrite = new float[] { 1.23f, 4.56f, 7.89f };
await client.WriteMultipleRegistersAsync(unitId: 1, startAddress: 0, valuesToWrite);

// 写入单个双精度浮点数
await client.WriteMultipleRegistersAsync(unitId: 1, startAddress: 10, 123.456);
```

### 字节序和字序控制

```csharp
// 使用小端字节序读取寄存器
var registersLittleEndian = await client.ReadHoldingRegistersAsync(
    unitId: 1, 
    startAddress: 0, 
    quantity: 10, 
    byteOrder: ByteOrder.LittleEndian);

// 使用低字在前的字序写入寄存器
await client.WriteMultipleRegistersAsync(
    unitId: 1, 
    startAddress: 0, 
    values, 
    byteOrder: ByteOrder.BigEndian, 
    wordOrder: WordOrder.LowFirst);
```

## 📁 项目结构

```
ModbusLib/
├── src/
│   ├── Interfaces/          # 核心接口定义
│   │   ├── IModbusClient.cs     # Modbus客户端接口
│   │   ├── IModbusProtocol.cs   # 协议处理接口
│   │   └── IModbusTransport.cs  # 传输层接口
│   ├── Enums/              # 枚举类型
│   │   ├── ByteOrder.cs         # 字节序枚举
│   │   ├── WordOrder.cs         # 字序枚举
│   │   ├── ModbusException.cs   # Modbus异常码
│   │   └── ModbusFunction.cs    # Modbus功能码
│   ├── Models/             # 数据模型
│   │   ├── ModbusRequest.cs     # Modbus请求模型
│   │   ├── ModbusResponse.cs    # Modbus响应模型
│   │   ├── NetworkConnectionConfig.cs  # 网络连接配置
│   │   └── SerialConnectionConfig.cs   # 串口连接配置
│   ├── Transports/         # 传输层实现
│   │   ├── SerialTransport.cs  # 串口传输实现
│   │   ├── TcpTransport.cs     # TCP传输实现
│   │   └── UdpTransport.cs     # UDP传输实现
│   ├── Protocols/          # 协议处理层
│   │   ├── RtuProtocol.cs       # RTU协议实现
│   │   └── TcpProtocol.cs       # TCP协议实现
│   ├── Clients/            # 客户端实现
│   │   ├── ModbusClientBase.cs     # 客户端基类
│   │   ├── ModbusRtuClient.cs      # RTU客户端实现
│   │   ├── ModbusTcpClient.cs      # TCP客户端实现
│   │   ├── ModbusUdpClient.cs      # UDP客户端实现
│   │   ├── ModbusRtuOverTcpClient.cs  # RTU over TCP客户端实现
│   │   └── ModbusRtuOverUdpClient.cs  # RTU over UDP客户端实现
│   ├── Factories/          # 工厂模式
│   │   ├── ModbusClientFactory.cs     # 客户端工厂
│   │   └── ConfigurationBuilders.cs   # 配置构建器
│   ├── Exceptions/         # 异常定义
│   │   ├── ModbusException.cs         # Modbus异常基类
│   │   └── ModbusCommunicationExceptions.cs  # 通信异常
│   └── Utils/              # 工具类
│       ├── Crc16Utils.cs        # CRC16校验工具
│       └── DataConverter.cs     # 数据转换工具
└── ModbusLib.Tests/        # 单元测试
    ├── Functional/         # 功能测试
    ├── Mocks/              # 模拟对象
    └── Utils/              # 测试工具
```

## 🔧 构建和测试

```bash
# 构建项目
dotnet build

# 运行测试
dotnet test

# 发布 NuGet 包
dotnet pack
```

## 📖 API 参考

### 工厂方法

```csharp
// 创建RTU客户端
IModbusClient rtuClient = ModbusClientFactory.CreateRtuClient("COM1", 9600);
// 或使用完整配置
IModbusClient rtuClient = ModbusClientFactory.CreateRtuClient(new SerialConnectionConfig {
    PortName = "COM1",
    BaudRate = 9600,
    Parity = Parity.None,
    DataBits = 8,
    StopBits = StopBits.One
});

// 创建TCP客户端
IModbusClient tcpClient = ModbusClientFactory.CreateTcpClient("192.168.1.100", 502);
// 或使用完整配置
IModbusClient tcpClient = ModbusClientFactory.CreateTcpClient(new NetworkConnectionConfig {
    Host = "192.168.1.100",
    RemotePort = 502,
    ConnectTimeout = 5000,
    ReceiveTimeout = 3000,
    SendTimeout = 3000
});

// 创建UDP客户端
IModbusClient udpClient = ModbusClientFactory.CreateUdpClient("192.168.1.100", 502);

// 创建RTU over TCP客户端
IModbusClient rtuOverTcpClient = ModbusClientFactory.CreateRtuOverTcpClient("192.168.1.100", 502);

// 创建RTU over UDP客户端
IModbusClient rtuOverUdpClient = ModbusClientFactory.CreateRtuOverUdpClient("192.168.1.100", 502);
```

### 支持的功能码

| 功能码 | 名称 | 描述 |
|--------|------|------|
| 0x01 | Read Coils | 读取线圈状态 |
| 0x02 | Read Discrete Inputs | 读取离散输入状态 |
| 0x03 | Read Holding Registers | 读取保持寄存器 |
| 0x04 | Read Input Registers | 读取输入寄存器 |
| 0x05 | Write Single Coil | 写单个线圈 |
| 0x06 | Write Single Register | 写单个寄存器 |
| 0x0F | Write Multiple Coils | 写多个线圈 |
| 0x10 | Write Multiple Registers | 写多个寄存器 |
| 0x17 | Read/Write Multiple Registers | 读写多个寄存器 |

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 MIT 许可证。