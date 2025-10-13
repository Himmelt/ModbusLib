# ModbusLib NuGet 包发布工具

这个工具用于自动化发布 ModbusLib 库到 NuGet.org。

## 使用方法

1. 确保已设置 NuGet API 密钥环境变量：
   ```powershell
   $env:NUGET_API_KEY="your-api-key"
   ```

2. 运行部署工具：
   ```powershell
   dotnet run
   ```

3. 按照提示输入版本号和版本描述信息

4. 确认发布信息后，工具将自动执行以下操作：
   - 更新 `ModbusLib.csproj` 文件中的版本号和发布说明
   - 清理之前的构建
   - 恢复依赖项
   - 运行测试
   - 构建项目
   - 打包项目
   - 发布到 NuGet.org

## 注意事项

- 确保在运行此工具之前已经正确配置了 NuGet API 密钥
- 工具会自动运行测试，确保所有测试通过后再发布
- 发布前会要求确认版本信息