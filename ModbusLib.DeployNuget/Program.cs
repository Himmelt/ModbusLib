using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// 获取用户输入的版本号和版本描述
Console.WriteLine("=== ModbusLib NuGet 包发布工具 ===");
Console.Write("请输入版本号 (例如: 1.0.0): ");
string? version = Console.ReadLine();

if (string.IsNullOrWhiteSpace(version))
{
    Console.WriteLine("错误: 版本号不能为空。");
    return 1;
}

Console.Write("请输入版本描述信息: ");
string? releaseNotes = Console.ReadLine();

if (string.IsNullOrWhiteSpace(releaseNotes))
{
    Console.WriteLine("错误: 版本描述信息不能为空。");
    return 1;
}

Console.WriteLine($"\n准备发布版本 {version}，描述信息: {releaseNotes}");
Console.Write("确认发布? (y/N): ");
string? confirm = Console.ReadLine();

if (!string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("发布已取消。");
    return 0;
}

try
{
    // 更新 ModbusLib.csproj 文件
    string csprojPath = "../ModbusLib/ModbusLib.csproj";
    await UpdateCsprojFileAsync(csprojPath, version, releaseNotes);
    
    // 执行构建、测试、打包、发布
    int result = await BuildTestPackAndPublishAsync(version);
    
    if (result == 0)
    {
        Console.WriteLine("\nNuGet 包发布成功!");
        return 0;
    }
    else
    {
        Console.WriteLine("\nNuGet 包发布失败!");
        return result;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"发生错误: {ex.Message}");
    return 1;
}

async Task UpdateCsprojFileAsync(string csprojPath, string version, string releaseNotes)
{
    Console.WriteLine("正在更新项目文件...");
    
    // 使用 UTF-8 编码读取文件内容
    string content = await File.ReadAllTextAsync(csprojPath, Encoding.UTF8);
    
    // 更新版本号
    content = Regex.Replace(content, @"<Version>.*</Version>", $"<Version>{version}</Version>");
    
    // 更新发布说明
    content = Regex.Replace(content, @"<PackageReleaseNotes>.*</PackageReleaseNotes>", $"<PackageReleaseNotes>{releaseNotes}</PackageReleaseNotes>");
    
    // 使用 UTF-8 编码写回文件
    await File.WriteAllTextAsync(csprojPath, content, Encoding.UTF8);
    
    Console.WriteLine("项目文件更新完成。");
}

async Task<int> BuildTestPackAndPublishAsync(string version)
{
    Console.WriteLine("开始执行构建、测试、打包和发布流程...");
    
    // 设置工作目录为解决方案根目录
    string solutionRoot = "..";
    
    // 1. 清理之前的构建
    Console.WriteLine("1. 清理之前的构建...");
    if (await RunCommandAsync("dotnet", "clean --configuration Release", solutionRoot) != 0)
    {
        Console.WriteLine("清理失败!");
        return 1;
    }
    
    // 2. 恢复依赖项
    Console.WriteLine("2. 恢复依赖项...");
    if (await RunCommandAsync("dotnet", "restore", solutionRoot) != 0)
    {
        Console.WriteLine("依赖项恢复失败!");
        return 1;
    }
    
    // 3. 运行测试
    Console.WriteLine("3. 运行测试...");
    if (await RunCommandAsync("dotnet", "test", solutionRoot) != 0)
    {
        Console.WriteLine("测试失败!");
        return 1;
    }
    
    // 4. 构建项目
    Console.WriteLine("4. 构建项目...");
    if (await RunCommandAsync("dotnet", "build ModbusLib/ModbusLib.csproj --configuration Release", solutionRoot) != 0)
    {
        Console.WriteLine("构建失败!");
        return 1;
    }
    
    // 5. 打包项目
    Console.WriteLine("5. 打包项目...");
    if (await RunCommandAsync("dotnet", "pack ModbusLib/ModbusLib.csproj --configuration Release --output nupkg --no-build", solutionRoot) != 0)
    {
        Console.WriteLine("打包失败!");
        return 1;
    }
    
    // 6. 查找生成的包文件
    Console.WriteLine("6. 查找生成的包文件...");
    string nupkgDirectory = Path.Combine(solutionRoot, "nupkg");
    if (!Directory.Exists(nupkgDirectory))
    {
        Console.WriteLine("错误: 未找到 nupkg 目录。");
        return 1;
    }
    
    var packageFiles = Directory.GetFiles(nupkgDirectory, "*.nupkg")
                                .Where(f => !f.EndsWith(".snupkg"))
                                .ToArray();
    
    if (packageFiles.Length == 0)
    {
        Console.WriteLine("错误: 未找到生成的 NuGet 包文件。");
        return 1;
    }
    
    string packageFile = packageFiles[0];
    Console.WriteLine($"找到包文件: {packageFile}");
    
    // 7. 发布到 NuGet
    Console.WriteLine("7. 发布到 NuGet...");
    string nugetSource = "https://api.nuget.org/v3/index.json";
    string apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY") ?? "";
    
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.WriteLine("错误: 未设置 NuGet API 密钥环境变量 NUGET_API_KEY。");
        return 1;
    }
    
    int result = await RunCommandAsync("dotnet", $"nuget push \"{packageFile}\" --api-key {apiKey} --source {nugetSource}", solutionRoot);
    
    if (result == 0)
    {
        Console.WriteLine($"NuGet 包 {version} 发布成功!");
    }
    else
    {
        Console.WriteLine("NuGet 包发布失败!");
    }
    
    return result;
}

async Task<int> RunCommandAsync(string command, string arguments, string workingDirectory = ".")
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = Path.GetFullPath(workingDirectory),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        }
    };
    
    process.Start();
    
    // 异步读取输出
    string output = await process.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();
    
    await process.WaitForExitAsync();
    
    // 输出命令结果（仅在有输出时）
    if (!string.IsNullOrWhiteSpace(output))
        Console.WriteLine(output);
    
    if (!string.IsNullOrWhiteSpace(error))
        Console.WriteLine($"错误: {error}");
    
    return process.ExitCode;
}
