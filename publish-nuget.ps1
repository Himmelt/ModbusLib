# ModbusLib NuGet 包一键发布脚本
# 使用方法: 
# 1. 确保已设置 NuGet API 密钥环境变量: $env:NUGET_API_KEY="your-api-key"
# 2. 在 PowerShell 中运行此脚本: .\publish-nuget.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$NugetSource = "https://api.nuget.org/v3/index.json",
    
    [Parameter(Mandatory=$false)]
    [switch]$Force = $false  # 跳过确认步骤
)

# 检查是否设置了 API 密钥
if (-not $env:NUGET_API_KEY) {
    Write-Host "错误: 未设置 NuGet API 密钥环境变量。" -ForegroundColor Red
    Write-Host "请先设置环境变量: `$env:NUGET_API_KEY=""your-api-key""" -ForegroundColor Yellow
    Write-Host "或者在命令行中设置: set NUGET_API_KEY=your-api-key" -ForegroundColor Yellow
    exit 1
}

# 检查 .NET SDK 是否安装
try {
    $dotnetVersion = dotnet --version
    Write-Host "检测到 .NET SDK 版本: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "错误: 未检测到 .NET SDK。请先安装 .NET SDK 9.0 或更高版本。" -ForegroundColor Red
    exit 1
}

# 显示项目信息
$csprojPath = "ModbusLib\ModbusLib.csproj"
if (Test-Path $csprojPath) {
    [xml]$csprojXml = Get-Content $csprojPath
    $packageName = $csprojXml.Project.PropertyGroup.PackageId
    $currentVersion = $csprojXml.Project.PropertyGroup.Version
    $packageDescription = $csprojXml.Project.PropertyGroup.Description
    
    Write-Host "`n=== 项目信息 ===" -ForegroundColor Cyan
    Write-Host "包名称: $packageName" -ForegroundColor White
    Write-Host "当前版本: $currentVersion" -ForegroundColor White
    Write-Host "描述: $packageDescription" -ForegroundColor White
    Write-Host "==================`n" -ForegroundColor Cyan
}

# 运行测试（除非跳过）
if (-not $SkipTests) {
    Write-Host "正在运行测试..." -ForegroundColor Cyan
    dotnet test
    if ($LASTEXITCODE -ne 0) {
        Write-Host "错误: 测试失败，发布已中止。" -ForegroundColor Red
        exit 1
    }
    Write-Host "测试通过!" -ForegroundColor Green
}

# 更新版本号（如果提供了版本号）
if ($Version) {
    Write-Host "正在更新版本号为: $Version" -ForegroundColor Cyan
    
    $csprojContent = Get-Content $csprojPath -Raw
    
    # 使用正则表达式替换版本号
    $updatedContent = $csprojContent -replace '<Version>.*</Version>', "<Version>$Version</Version>"
    
    # 保存更新后的内容
    Set-Content $csprojPath $updatedContent
    
    Write-Host "版本号已更新为: $Version" -ForegroundColor Green
    $currentVersion = $Version
}

# 清理之前的构建
Write-Host "正在清理之前的构建..." -ForegroundColor Cyan
dotnet clean --configuration Release | Out-Null

# 恢复依赖项
Write-Host "正在恢复依赖项..." -ForegroundColor Cyan
dotnet restore | Out-Null

# 创建输出目录
$outputDir = "nupkg"
if (Test-Path $outputDir) {
    Remove-Item "$outputDir\*" -Recurse -Force
} else {
    New-Item -ItemType Directory -Name $outputDir | Out-Null
}

# 发布前确认
if (-not $Force) {
    Write-Host "`n=== 发布预览 ===" -ForegroundColor Yellow
    Write-Host "即将发布的包信息:" -ForegroundColor Yellow
    Write-Host "  包名称: $packageName" -ForegroundColor White
    Write-Host "  版本号: $currentVersion" -ForegroundColor White
    Write-Host "  输出目录: $outputDir" -ForegroundColor White
    Write-Host "  NuGet 源: $NugetSource" -ForegroundColor White
    Write-Host "==================`n" -ForegroundColor Yellow
    
    $confirm = Read-Host "确认发布? (y/N)"
    if ($confirm -ne "y" -and $confirm -ne "Y") {
        Write-Host "发布已取消。" -ForegroundColor Yellow
        exit 0
    }
}

# 打包 NuGet 包
Write-Host "正在创建 NuGet 包..." -ForegroundColor Cyan
dotnet build --configuration Release | Out-Null
dotnet pack --configuration Release --output $outputDir --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Host "错误: 打包失败。" -ForegroundColor Red
    exit 1
}

# 获取生成的包文件
$packageFiles = Get-ChildItem -Path $outputDir -Filter "*.nupkg" | Where-Object { $_.Name -notlike "*.snupkg" }
if ($packageFiles.Count -eq 0) {
    Write-Host "错误: 未找到生成的 NuGet 包文件。" -ForegroundColor Red
    exit 1
}

$packageFile = $packageFiles[0].FullName
Write-Host "找到包文件: $packageFile" -ForegroundColor Green

# 发布到 NuGet 源
Write-Host "正在发布到 NuGet..." -ForegroundColor Cyan
dotnet nuget push $packageFile --api-key $env:NUGET_API_KEY --source $NugetSource

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nNuGet 包发布成功!" -ForegroundColor Green
    Write-Host "包文件: $($packageFiles[0].Name)" -ForegroundColor Green
    Write-Host "版本号: $currentVersion" -ForegroundColor Green
    Write-Host "NuGet 源: $NugetSource" -ForegroundColor Green
} else {
    Write-Host "错误: NuGet 包发布失败。" -ForegroundColor Red
    exit 1
}