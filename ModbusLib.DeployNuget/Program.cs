using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// 获取用户输入的版本号和版本描述
Console.WriteLine("=== ModbusLib NuGet 包发布工具 ===");

// 先读取当前版本号
string csprojPath = "../ModbusLib/ModbusLib.csproj";
string currentVersion = await GetCurrentVersionAsync(csprojPath);
Console.WriteLine($"当前版本号: {currentVersion}");
Console.Write($"请输入新版本号: ");

string? version = Console.ReadLine();

if (string.IsNullOrWhiteSpace(version)) {
    Console.WriteLine("错误: 版本号不能为空。");
    return 1;
}

Console.Write("请输入版本描述信息: ");
string? releaseNotes = Console.ReadLine();

if (string.IsNullOrWhiteSpace(releaseNotes)) {
    Console.WriteLine("错误: 版本描述信息不能为空。");
    return 1;
}

// 显示发布预览信息
Console.WriteLine("\n==== 发布预览 ====");
// 获取并显示NuGet包的所有信息
var packageInfo = await GetPackageInfoAsync(csprojPath);
Console.WriteLine($"包ID: {packageInfo.Id}");
Console.WriteLine($"版本: {version}");
Console.WriteLine($"作者: {packageInfo.Authors}");
Console.WriteLine($"公司: {packageInfo.Company}");
Console.WriteLine($"标题: {packageInfo.Title}");
Console.WriteLine($"描述: {packageInfo.Description}");
Console.WriteLine($"版权: {packageInfo.Copyright}");
Console.WriteLine($"项目URL: {packageInfo.ProjectUrl}");
Console.WriteLine($"仓库URL: {packageInfo.RepositoryUrl}");
Console.WriteLine($"标签: {packageInfo.Tags}");
Console.WriteLine($"许可证: {packageInfo.License}");
Console.WriteLine($"版本描述: {releaseNotes}");

// 显示将要发布的文件路径
string solutionRoot = "..";
string nupkgDirectory = Path.Combine(solutionRoot, "nupkg");
string packageFileName = $"Himmelt.ModbusLib.{version}.nupkg";
string symbolPackageFileName = $"Himmelt.ModbusLib.{version}.snupkg";
string packageFilePath = Path.Combine(nupkgDirectory, packageFileName);
string symbolPackageFilePath = Path.Combine(nupkgDirectory, symbolPackageFileName);
Console.WriteLine($"主包文件路径: {packageFilePath}");
Console.WriteLine($"符号包文件路径: {symbolPackageFilePath}");

Console.WriteLine("==================");

Console.Write("确认发布? (y/N): ");
string? confirm = Console.ReadLine();

if (!string.Equals(confirm, "y", StringComparison.OrdinalIgnoreCase)) {
    Console.WriteLine("发布已取消。");
    return 0;
}

try {
    // 更新 ModbusLib.csproj 文件
    await UpdateCsprojFileAsync(csprojPath, version, releaseNotes);

    // 执行构建、测试、打包、发布
    int result = await BuildTestPackAndPublishAsync(version);

    if (result == 0) {
        Console.WriteLine("\nNuGet 包发布成功!");
        return 0;
    } else {
        Console.WriteLine("\nNuGet 包发布失败!");
        return result;
    }
} catch (Exception ex) {
    Console.WriteLine($"发生错误: {ex.Message}");
    return 1;
}

async Task<string> GetCurrentVersionAsync(string csprojPath) {
    try {
        // 使用 UTF-8 编码读取文件内容
        string content = await File.ReadAllTextAsync(csprojPath, Encoding.UTF8);

        // 使用正则表达式提取版本号
        var match = Regex.Match(content, @"<Version>(.*?)</Version>");
        if (match.Success) {
            return match.Groups[1].Value;
        }
    } catch (Exception ex) {
        Console.WriteLine($"读取当前版本号时发生错误: {ex.Message}");
    }

    return "未知";
}

// 新增函数：获取NuGet包的所有信息
async Task<PackageInfo> GetPackageInfoAsync(string csprojPath) {
    try {
        // 使用 UTF-8 编码读取文件内容
        string content = await File.ReadAllTextAsync(csprojPath, Encoding.UTF8);

        // 提取所有包信息
        var packageInfo = new PackageInfo();

        var idMatch = Regex.Match(content, @"<PackageId>(.*?)</PackageId>");
        packageInfo.Id = idMatch.Success ? idMatch.Groups[1].Value : "未知";

        var authorsMatch = Regex.Match(content, @"<Authors>(.*?)</Authors>");
        packageInfo.Authors = authorsMatch.Success ? authorsMatch.Groups[1].Value : "未知";

        var companyMatch = Regex.Match(content, @"<Company>(.*?)</Company>");
        packageInfo.Company = companyMatch.Success ? companyMatch.Groups[1].Value : "未知";

        var titleMatch = Regex.Match(content, @"<Title>(.*?)</Title>");
        packageInfo.Title = titleMatch.Success ? titleMatch.Groups[1].Value : "未知";

        var descriptionMatch = Regex.Match(content, @"<Description>(.*?)</Description>");
        packageInfo.Description = descriptionMatch.Success ? descriptionMatch.Groups[1].Value : "未知";

        var copyrightMatch = Regex.Match(content, @"<Copyright>(.*?)</Copyright>");
        packageInfo.Copyright = copyrightMatch.Success ? copyrightMatch.Groups[1].Value : "未知";

        var projectUrlMatch = Regex.Match(content, @"<PackageProjectUrl>(.*?)</PackageProjectUrl>");
        packageInfo.ProjectUrl = projectUrlMatch.Success ? projectUrlMatch.Groups[1].Value : "未知";

        var repositoryUrlMatch = Regex.Match(content, @"<RepositoryUrl>(.*?)</RepositoryUrl>");
        packageInfo.RepositoryUrl = repositoryUrlMatch.Success ? repositoryUrlMatch.Groups[1].Value : "未知";

        var tagsMatch = Regex.Match(content, @"<PackageTags>(.*?)</PackageTags>");
        packageInfo.Tags = tagsMatch.Success ? tagsMatch.Groups[1].Value : "未知";

        var licenseMatch = Regex.Match(content, @"<PackageLicenseExpression>(.*?)</PackageLicenseExpression>");
        packageInfo.License = licenseMatch.Success ? licenseMatch.Groups[1].Value : "未知";

        return packageInfo;
    } catch (Exception ex) {
        Console.WriteLine($"读取包信息时发生错误: {ex.Message}");
        return new PackageInfo();
    }
}

async Task UpdateCsprojFileAsync(string csprojPath, string version, string releaseNotes) {
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

async Task<int> BuildTestPackAndPublishAsync(string version) {
    Console.WriteLine("开始执行构建、测试、打包和发布流程...");

    // 设置工作目录为解决方案根目录
    string solutionRoot = "..";

    // 1. 清理之前的构建
    Console.WriteLine("1. 清理之前的构建...");
    if (await RunCommandAsync("dotnet", "clean --configuration Release", solutionRoot) != 0) {
        Console.WriteLine("清理失败!");
        return 1;
    }

    // 2. 恢复依赖项
    Console.WriteLine("2. 恢复依赖项...");
    if (await RunCommandAsync("dotnet", "restore", solutionRoot) != 0) {
        Console.WriteLine("依赖项恢复失败!");
        return 1;
    }

    // 3. 运行测试
    Console.WriteLine("3. 运行测试...");
    if (await RunCommandAsync("dotnet", "test", solutionRoot) != 0) {
        Console.WriteLine("测试失败!");
        return 1;
    }

    // 4. 构建项目
    Console.WriteLine("4. 构建项目...");
    if (await RunCommandAsync("dotnet", "build ModbusLib/ModbusLib.csproj --configuration Release", solutionRoot) != 0) {
        Console.WriteLine("构建失败!");
        return 1;
    }

    // 5. 打包项目
    Console.WriteLine("5. 打包项目...");
    if (await RunCommandAsync("dotnet", "pack ModbusLib/ModbusLib.csproj --configuration Release --output nupkg --no-build", solutionRoot) != 0) {
        Console.WriteLine("打包失败!");
        return 1;
    }

    // 6. 查找生成的包文件
    Console.WriteLine("6. 查找生成的包文件...");
    string nupkgDirectory = Path.Combine(solutionRoot, "nupkg");
    if (!Directory.Exists(nupkgDirectory)) {
        Console.WriteLine("错误: 未找到 nupkg 目录。");
        return 1;
    }

    // 查找 .nupkg 文件（排除 .snupkg 文件）
    var packageFiles = Directory.GetFiles(nupkgDirectory, "*.nupkg")
                                .Where(f => !f.EndsWith(".snupkg"))
                                .ToArray();

    if (packageFiles.Length == 0) {
        Console.WriteLine("错误: 未找到生成的 NuGet 包文件。");
        return 1;
    }

    string packageFile = packageFiles[0];
    Console.WriteLine($"找到主包文件: {packageFile}");

    // 查找 .snupkg symbol 包文件
    var symbolPackageFiles = Directory.GetFiles(nupkgDirectory, "*.snupkg").ToArray();

    string? symbolPackageFile = null;
    if (symbolPackageFiles.Length > 0) {
        symbolPackageFile = symbolPackageFiles[0];
        Console.WriteLine($"找到符号包文件: {symbolPackageFile}");
    } else {
        Console.WriteLine("警告: 未找到生成的符号包文件。");
    }

    // 7. 发布到 NuGet
    Console.WriteLine("7. 发布到 NuGet...");
    string nugetSource = "https://api.nuget.org/v3/index.json";
    string apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY") ?? "";

    if (string.IsNullOrWhiteSpace(apiKey)) {
        Console.WriteLine("错误: 未设置 NuGet API 密钥环境变量 NUGET_API_KEY。");
        return 1;
    }

    // 发布主包
    Console.WriteLine("发布主包...");
    int result = await RunCommandAsync("dotnet", $"nuget push \"{packageFile}\" --api-key {apiKey} --source {nugetSource}", solutionRoot);

    if (result != 0) {
        Console.WriteLine("NuGet 主包发布失败!");
        return result;
    }

    Console.WriteLine($"NuGet 主包 {version} 发布成功!");

    // 如果存在符号包，则发布符号包
    if (!string.IsNullOrEmpty(symbolPackageFile)) {
        Console.WriteLine("发布符号包...");
        result = await RunCommandAsync("dotnet", $"nuget push \"{symbolPackageFile}\" --api-key {apiKey} --source {nugetSource}", solutionRoot);

        if (result != 0) {
            Console.WriteLine("NuGet 符号包发布失败!");
            return result;
        }

        Console.WriteLine($"NuGet 符号包 {version} 发布成功!");
    }

    return result;
}

async Task<int> RunCommandAsync(string command, string arguments, string workingDirectory = ".") {
    var process = new Process {
        StartInfo = new ProcessStartInfo {
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

// PackageInfo类定义
class PackageInfo {
    public string Id { get; set; } = "未知";
    public string Authors { get; set; } = "未知";
    public string Company { get; set; } = "未知";
    public string Title { get; set; } = "未知";
    public string Description { get; set; } = "未知";
    public string Copyright { get; set; } = "未知";
    public string ProjectUrl { get; set; } = "未知";
    public string RepositoryUrl { get; set; } = "未知";
    public string Tags { get; set; } = "未知";
    public string License { get; set; } = "未知";
}
