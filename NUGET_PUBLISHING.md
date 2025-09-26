# NuGet 包发布指南

本指南将帮助您将 ModbusLib 库发布到 NuGet 包管理器上。

## 前提条件

在开始之前，请确保您已经完成以下准备工作：

1. 已安装 [.NET SDK 9.0 或更高版本](https://dotnet.microsoft.com/download)
2. 已在 [NuGet.org](https://www.nuget.org/) 注册账号
3. 已获取 NuGet API 密钥（可在 [NuGet.org 账户设置](https://www.nuget.org/account/apikeys) 中创建）

## 发布前准备

### 1. 更新版本号

在 `ModbusLib.csproj` 文件中更新版本号：

```xml
<Version>1.0.0</Version> <!-- 更新为新版本号 -->
```

遵循语义化版本控制（SemVer）原则：
- 主版本号（Major）：不兼容的API变更
- 次版本号（Minor）：向下兼容的功能性新增
- 修订号（Patch）：向下兼容的问题修正

### 2. 检查元数据

确保 `ModbusLib.csproj` 文件中的所有元数据都是最新的：
- 包描述
- 作者信息
- 项目URL
- 许可证信息
- 标签

### 3. 运行测试

在发布前，确保所有测试都通过：

```bash
dotnet test
```

## 打包 NuGet 包

### 创建发布版本的 NuGet 包

```bash
dotnet pack --configuration Release --output ./nupkg
```

这个命令将在项目根目录下创建一个 `nupkg` 文件夹，并生成以下文件：
- `ModbusLib.1.0.0.nupkg`（主包文件）
- `ModbusLib.1.0.0.snupkg`（符号包文件，包含调试信息）

### 可选：打包时跳过测试

如果您确定不需要运行测试，可以使用以下命令：

```bash
dotnet pack --configuration Release --output ./nupkg --no-build
```

## 发布到 NuGet.org

### 使用 .NET CLI 发布

```bash
dotnet nuget push ./nupkg/ModbusLib.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

请将 `YOUR_API_KEY` 替换为您在 NuGet.org 获取的实际 API 密钥。

### 发布符号包

上面的命令会自动发布主包和符号包。如果需要单独发布符号包，可以使用：

```bash
dotnet nuget push ./nupkg/ModbusLib.1.0.0.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## 验证发布

发布成功后，您可以在 [NuGet.org](https://www.nuget.org/) 上搜索您的包来验证发布是否成功。通常需要几分钟时间，包才会在搜索结果中显示。

## 发布到私有 NuGet 源

如果您需要发布到私有 NuGet 源，只需更改 `--source` 参数：

```bash
dotnet nuget push ./nupkg/ModbusLib.1.0.0.nupkg --api-key YOUR_API_KEY --source https://your-private-nuget-source/v3/index.json
```

## 发布注意事项

1. **API 密钥安全**：不要将 API 密钥存储在代码仓库中，建议使用环境变量或安全的密钥管理系统。

2. **版本号唯一性**：NuGet.org 不允许重复发布相同版本号的包。如果需要更新包，必须增加版本号。

3. **发布前测试**：建议在发布前先发布到测试源，或者使用 [nuget.org 的预发布功能](https://learn.microsoft.com/en-us/nuget/create-packages/prerelease-packages)。

4. **包大小优化**：确保包尽可能小，只包含必要的文件。

## 参考资源

- [NuGet 文档](https://learn.microsoft.com/en-us/nuget/)
- [创建 NuGet 包](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package)
- [发布 NuGet 包](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [语义化版本控制规范](https://semver.org/lang/zh-CN/)