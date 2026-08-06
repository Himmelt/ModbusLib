<div align="center">

# ModbusLib
一个功能完整、高性能的 Modbus 客户端库，支持 .NET 8 / 9 / 10 平台和多种 Modbus 协议变体

[![CI](https://github.com/Himmelt/ModbusLib/actions/workflows/ci.yml/badge.svg)](https://github.com/Himmelt/ModbusLib/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Himmelt.ModbusLib?color=004880&logo=nuget)](https://www.nuget.org/packages/Himmelt.ModbusLib)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Himmelt.ModbusLib?color=004880&logo=nuget)](https://www.nuget.org/packages/Himmelt.ModbusLib)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/Himmelt/ModbusLib?color=green)](LICENSE)
[![DeepSeek](https://img.shields.io/badge/DeepSeek-4D6BFE?logo=deepseek&logoColor=white)](https://www.deepseek.com/)

</div>

> **注意**：本项目由 AI 辅助生成，主要使用了 Trae、Codex 以及 Qoder 等工具，主要模型是 Qwen-3-Coder、Qwen-3.5-Plus、MiniMax-2.7、GLM-5.2、DeepSeek-V4 等。

## 📦 安装

```bash
dotnet add package Himmelt.ModbusLib
```

## 🌟 支持的协议和传输方式

### 协议类型

- **Modbus RTU** - 基于 RTU 帧格式的 Modbus 协议，使用 CRC16 校验
- **Modbus TCP** - 基于 TCP/IP 的标准 Modbus 协议，使用 MBAP 报文头

### 传输方式

- **Serial (串口)** - 通过串口进行 RTU 通信
- **TCP** - 通过 TCP 网络进行 TCP 或 RTU 帧传输
- **UDP** - 通过 UDP 网络进行 TCP 或 RTU 帧传输
- **Pipe** - 通过 .NET Pipe 进行自定义数据流传输
- **Channel** - 通过 .NET Channel 进行自定义数据流传输

### 协议与传输组合示例

| 传输方式 | RTU 协议 | TCP 协议 |
|---------|---------|---------|
| **Serial** | ✅ ModbusRtuClient | ❌ |
| **TCP** | ✅ ModbusTcpClient (ProtocolType.Rtu) | ✅ ModbusTcpClient (ProtocolType.Tcp) |
| **UDP** | ✅ ModbusUdpClient (ProtocolType.Rtu) | ✅ ModbusUdpClient (ProtocolType.Tcp) |
| **Pipe** | ✅ ModbusPipeClient (ProtocolType.Rtu) | ✅ ModbusPipeClient (ProtocolType.Tcp) |
| **Channel** | ✅ ModbusChannelClient (ProtocolType.Rtu) | ✅ ModbusChannelClient (ProtocolType.Tcp) |

## ✨ 功能特性

- 🚀 **异步编程模型**，提供卓越的性能
- 🔄 **同步方法支持**，便于传统应用集成
- 📋 **支持所有标准 Modbus 功能码**
- ⚙️ **灵活的配置选项**
- 🛡️ **完整的错误处理和重试机制**
- 💾 **内存优化**，使用 ArrayPool 减少分配
- 🔀 **泛型支持**，可直接读写基本数据类型（byte, short, int, long, float, double 等）
- 📊 **字节序和字序控制**，支持不同平台数据格式
- 🔌 **多种传输方式**，支持串口、TCP、UDP、Pipe、Channel
- 🎛️ **协议可选**，通过网络客户端可灵活选择 RTU 或 TCP 协议

## 🚀 快速开始

### 基本用法

#### 串口 RTU 客户端

```csharp
using ModbusLib.Clients;
using ModbusLib.Models;

// 创建 RTU 客户端（串口传输，默认使用 RTU 协议）
var rtuClient = new ModbusRtuClient(new SerialConfig
{
    PortName = "COM1",
    BaudRate = 9600,
    Parity = System.IO.Ports.Parity.None,
    DataBits = 8,
    StopBits = System.IO.Ports.StopBits.One
});

// 连接并读取数据
await rtuClient.ConnectAsync();
var coils = await rtuClient.ReadCoilsAsync(unitId: 1, startAddress: 0, quantity: 10);
```

#### 网络客户端（可切换协议）

```csharp
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Models;

// TCP 传输 + TCP 协议（标准 Modbus TCP）
var tcpClient = new ModbusTcpClient(new NetworkConfig
{
    RemoteHost = "192.168.1.100",
    RemotePort = 502
}, protocol: ProtocolType.Tcp);  // 或省略 protocol 参数，默认为 Tcp

await tcpClient.ConnectAsync();
await tcpClient.WriteMultipleRegistersAsync(unitId: 1, startAddress: 0, new ushort[] { 100, 200, 300 });
```

```csharp
// TCP 传输 + RTU 协议（RTU over TCP）
var rtuOverTcpClient = new ModbusTcpClient(new NetworkConfig
{
    RemoteHost = "192.168.1.100",
    RemotePort = 502
}, protocol: ProtocolType.Rtu);

await rtuOverTcpClient.ConnectAsync();
var registers = await rtuOverTcpClient.ReadHoldingRegistersAsync(unitId: 1, startAddress: 0, quantity: 10);
```

```csharp
// UDP 传输 + RTU 协议（RTU over UDP）
var rtuOverUdpClient = new ModbusUdpClient(new NetworkConfig
{
    RemoteHost = "192.168.1.100",
    RemotePort = 502
}, protocol: ProtocolType.Rtu);

await rtuOverUdpClient.ConnectAsync();
var inputs = await rtuOverUdpClient.ReadDiscreteInputsAsync(unitId: 1, startAddress: 0, quantity: 8);
```

#### 同步方法示例

```csharp
// 串口 RTU 客户端（同步）
var rtuClient = new ModbusRtuClient("COM1", 9600);
rtuClient.Connect();
var coils = rtuClient.ReadCoils(unitId: 1, startAddress: 0, quantity: 10);
```

```csharp
// TCP 客户端 + RTU 协议（同步）
var rtuOverTcpClient = new ModbusTcpClient("192.168.1.100", 502, protocol: ProtocolType.Rtu);
rtuOverTcpClient.Connect();
var registers = rtuOverTcpClient.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 10);
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

### 使用 Pipe 和 Channel 客户端

```csharp
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Models;

// Pipe 客户端 + TCP 协议（默认）
var pipeSession = new PipeSession();
var pipeTcpClient = new ModbusPipeClient(pipeSession, protocol: ProtocolType.Tcp);
await pipeTcpClient.ConnectAsync();

// Pipe 客户端 + RTU 协议
var pipeRtuClient = new ModbusPipeClient(pipeSession, protocol: ProtocolType.Rtu);
await pipeRtuClient.ConnectAsync();

// Channel 客户端 + TCP 协议（默认）
var channelSession = new ChannelSession();
var channelTcpClient = new ModbusChannelClient(channelSession, protocol: ProtocolType.Tcp);
await channelTcpClient.ConnectAsync();

// Channel 客户端 + RTU 协议
var channelRtuClient = new ModbusChannelClient(channelSession, protocol: ProtocolType.Rtu);
await channelRtuClient.ConnectAsync();
```

> **提示**：Pipe 和 Channel 客户端适用于需要自定义数据源的场景，例如模拟设备、测试环境或通过其他传输层进行通信。

## 📁 项目结构

```
ModbusLib/
├── Clients/              # 客户端实现
│   ├── ModbusClientBase.cs       # 客户端基类
│   ├── ModbusRtuClient.cs        # RTU 客户端
│   ├── ModbusTcpClient.cs        # TCP 客户端
│   ├── ModbusUdpClient.cs        # UDP 客户端
│   ├── ModbusPipeClient.cs       # Pipe 客户端
│   └── ModbusChannelClient.cs    # Channel 客户端
├── Interfaces/           # 核心接口定义
│   ├── IModbusClient.cs      # Modbus 客户端接口
│   ├── IModbusProtocol.cs    # 协议处理接口
│   └── IModbusTransport.cs   # 传输层接口
├── Enums/                # 枚举类型
│   ├── ByteOrder.cs            # 字节序枚举
│   ├── WordOrder.cs            # 字序枚举
│   ├── ModbusFunction.cs       # Modbus 功能码
│   ├── ModbusExceptionCode.cs  # Modbus 异常码
│   └── ProtocolType.cs         # 协议类型
├── Models/               # 数据模型
│   ├── ModbusRequest.cs        # Modbus 请求模型
│   ├── ModbusResponse.cs       # Modbus 响应模型
│   ├── SerialConfig.cs         # 串口连接配置
│   ├── NetworkConfig.cs        # 网络连接配置
│   ├── PipeSession.cs          # Pipe 会话模型
│   └── ChannelSession.cs       # Channel 会话模型
├── Transports/           # 传输层实现
│   ├── SerialTransport.cs      # 串口传输实现
│   ├── TcpTransport.cs         # TCP 传输实现
│   ├── UdpTransport.cs         # UDP 传输实现
│   ├── PipeTransport.cs        # Pipe 传输实现
│   └── ChannelTransport.cs     # Channel 传输实现
├── Protocols/            # 协议处理层
│   ├── RtuProtocol.cs          # RTU 协议实现
│   └── TcpProtocol.cs          # TCP 协议实现
├── Exceptions/           # 异常定义
│   ├── ModbusException.cs            # Modbus 异常基类
│   └── ModbusCommunicationExceptions.cs  # 通信异常
└── Utils/                # 工具类
    ├── Crc16Utils.cs             # CRC16 校验工具
    ├── DataConverter.cs          # 数据转换工具
    └── CancelTokenExtensions.cs  # 取消令牌扩展

ModbusLib.Tests/        # 单元测试
├── Clients/              # 客户端测试
├── Functional/           # 功能测试
├── Mocks/                # 模拟对象
├── Transports/           # 传输层测试
└── Utils/                # 工具类测试
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

### 客户端类型

#### ModbusRtuClient（串口 RTU 客户端）

```csharp
using ModbusLib.Clients;
using ModbusLib.Models;

// 简化构造（推荐）
var rtuClient = new ModbusRtuClient("COM1", 9600);

// 或使用完整配置
var rtuClient = new ModbusRtuClient(new SerialConfig {
    PortName = "COM1",
    BaudRate = 9600,
    Parity = Parity.None,
    DataBits = 8,
    StopBits = StopBits.One
});
```

> **说明**：`ModbusRtuClient` 固定使用 RTU 协议，无需指定 `ProtocolType`

#### ModbusTcpClient（TCP 客户端，支持协议切换）

```csharp
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Models;

// TCP 传输 + TCP 协议（默认，标准 Modbus TCP）
var tcpClient = new ModbusTcpClient("192.168.1.100", 502);
// 或显式指定
var tcpClient = new ModbusTcpClient("192.168.1.100", 502, protocol: ProtocolType.Tcp);

// TCP 传输 + RTU 协议（RTU over TCP）
var rtuOverTcpClient = new ModbusTcpClient("192.168.1.100", 502, protocol: ProtocolType.Rtu);

// 使用完整配置
var tcpClient = new ModbusTcpClient(new NetworkConfig {
    RemoteHost = "192.168.1.100",
    RemotePort = 502,
    ConnectTimeout = 5000,
    ReceiveTimeout = 3000,
    SendTimeout = 3000
}, protocol: ProtocolType.Tcp);
```

#### ModbusUdpClient（UDP 客户端，支持协议切换）

```csharp
using ModbusLib.Clients;
using ModbusLib.Enums;

// UDP 传输 + TCP 协议（默认）
var udpClient = new ModbusUdpClient("192.168.1.100", 502);

// UDP 传输 + RTU 协议（RTU over UDP）
var rtuOverUdpClient = new ModbusUdpClient("192.168.1.100", 502, protocol: ProtocolType.Rtu);
```

#### ModbusPipeClient（Pipe 客户端，支持协议切换）

```csharp
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Models;

// Pipe + TCP 协议（默认）
var pipeSession = new PipeSession();
var pipeClient = new ModbusPipeClient(pipeSession);

// Pipe + RTU 协议
var pipeRtuClient = new ModbusPipeClient(pipeSession, protocol: ProtocolType.Rtu);
```

#### ModbusChannelClient（Channel 客户端，支持协议切换）

```csharp
using ModbusLib.Clients;
using ModbusLib.Enums;
using ModbusLib.Models;

// Channel + TCP 协议（默认）
var channelSession = new ChannelSession();
var channelClient = new ModbusChannelClient(channelSession);

// Channel + RTU 协议
var channelRtuClient = new ModbusChannelClient(channelSession, protocol: ProtocolType.Rtu);
```

### ProtocolType 枚举

```csharp
namespace ModbusLib.Enums;

public enum ProtocolType {
    Rtu,  // RTU 协议（使用 CRC16 校验）
    Tcp   // TCP 协议（使用 MBAP 报文头）
}
```

### 协议选择指南

| 客户端类型 | 默认协议 | 可选协议 | 典型应用场景 |
|-----------|---------|---------|-------------|
| **ModbusRtuClient** | RTU | ❌ 不可选 | 串口通信 |
| **ModbusTcpClient** | TCP | ✅ TCP/RTU | 以太网通信，RTU over TCP |
| **ModbusUdpClient** | TCP | ✅ TCP/RTU | UDP 广播/组播，RTU over UDP |
| **ModbusPipeClient** | TCP | ✅ TCP/RTU | 自定义数据流，测试模拟 |
| **ModbusChannelClient** | TCP | ✅ TCP/RTU | 自定义数据流，测试模拟 |

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

### 主要方法

#### 连接管理
- `ConnectAsync()` / `Connect()` - 连接到设备
- `DisconnectAsync()` / `Disconnect()` - 断开连接
- `IsConnected` - 获取连接状态

#### 读取功能
- `ReadCoilsAsync()` / `ReadCoils()` - 读取线圈状态
- `ReadDiscreteInputsAsync()` / `ReadDiscreteInputs()` - 读取离散输入
- `ReadHoldingRegistersAsync()` / `ReadHoldingRegisters()` - 读取保持寄存器
- `ReadInputRegistersAsync()` / `ReadInputRegisters()` - 读取输入寄存器
- `ReadHoldingRegistersRawAsync()` / `ReadHoldingRegistersRaw()` - 读取保持寄存器原始字节
- `ReadInputRegistersRawAsync()` / `ReadInputRegistersRaw()` - 读取输入寄存器原始字节

#### 写入功能
- `WriteSingleCoilAsync()` / `WriteSingleCoil()` - 写单个线圈
- `WriteMultipleCoilsAsync()` / `WriteMultipleCoils()` - 写多个线圈
- `WriteSingleRegisterAsync()` / `WriteSingleRegister()` - 写单个寄存器
- `WriteMultipleRegistersAsync()` / `WriteMultipleRegisters()` - 写多个寄存器
- `WriteMultipleRegistersRawAsync()` / `WriteMultipleRegistersRaw()` - 写原始字节到寄存器

#### 高级功能
- `ReadWriteMultipleRegistersAsync()` / `ReadWriteMultipleRegisters()` - 读写多个寄存器

#### 泛型方法
- `ReadHoldingRegistersAsync<T>()` / `ReadHoldingRegisters<T>()` - 泛型读取保持寄存器
- `ReadInputRegistersAsync<T>()` / `ReadInputRegisters<T>()` - 泛型读取输入寄存器
- `WriteMultipleRegistersAsync<T>()` / `WriteMultipleRegisters<T>()` - 泛型写入寄存器

### 配置选项

#### SerialConfig（串口配置）
```csharp
public class SerialConfig {
    public string PortName { get; set; }        // 串口名称 (如 COM1)
    public int BaudRate { get; set; }           // 波特率，默认 9600
    public Parity Parity { get; set; }          // 校验位，默认 None
    public int DataBits { get; set; }           // 数据位，默认 8
    public StopBits StopBits { get; set; }      // 停止位，默认 One
    public int ReadTimeout { get; set; }        // 读取超时（毫秒）
    public int WriteTimeout { get; set; }       // 写入超时（毫秒）
}
```

#### NetworkConfig（网络配置）
```csharp
public class NetworkConfig {
    public string RemoteHost { get; set; }      // 远程主机地址
    public int RemotePort { get; set; }         // 远程端口，默认 502
    public int ConnectTimeout { get; set; }     // 连接超时（毫秒）
    public int ReceiveTimeout { get; set; }     // 接收超时（毫秒）
    public int SendTimeout { get; set; }        // 发送超时（毫秒）
    public int ReceiveBufferSize { get; set; }  // 接收缓冲区大小
    public int SendBufferSize { get; set; }     // 发送缓冲区大小
    public string? LocalHost { get; set; }      // 本地主机地址（可选）
    public int? LocalPort { get; set; }         // 本地端口（可选）
}
```

### 超时与重试

- `Timeout`（客户端/传输层属性）为单次请求的整体超时，默认 `-1` 表示不启用；设为 `>= 0` 后生效（毫秒）。
- `NetworkConfig.ReceiveTimeout` / `SendTimeout`（串口对应 `ReadTimeout` / `WriteTimeout`）同样作用于异步收发，不再只是同步 I/O 的配置。
- 通信超时或异常后，TCP 传输会自动断开底层连接并清理可能残留的半帧数据，避免后续请求读到错位数据；配合 `Retries > 0` 时，客户端会在重试前自动重连。
- `Retries` 默认 `0`（不重试）。需要自动恢复时请显式设置，例如 `client.Retries = 2`。
- 触发重试的异常包括：`ModbusTimeoutException`、`ModbusCommunicationException`、`ModbusConnectionException` 以及设备忙（`TargetDeviceBusy`）异常。

### 数据类型转换

库支持所有 `unmanaged` 类型的泛型读写，包括但不限于：
- `byte`, `sbyte` - 8 位整数
- `short`, `ushort` - 16 位整数
- `int`, `uint` - 32 位整数
- `long`, `ulong` - 64 位整数
- `float` - 单精度浮点数
- `double` - 双精度浮点数
- 任何 `struct` 类型

```csharp
// 读取 10 个字节（占用 5 个寄存器）
var bytes = await client.ReadHoldingRegistersAsync<byte>(unitId: 1, startAddress: 0, count: 10);

// 读取 5 个整数（占用 10 个寄存器）
var ints = await client.ReadHoldingRegistersAsync<int>(unitId: 1, startAddress: 10, count: 5);

// 写入浮点数数组
float[] values = new float[] { 1.5f, 2.5f, 3.5f };
await client.WriteMultipleRegistersAsync(unitId: 1, startAddress: 0, values);
```

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目采用 MIT 许可证。
