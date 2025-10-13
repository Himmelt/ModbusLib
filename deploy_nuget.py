#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import asyncio
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Optional, Tuple
import xml.etree.ElementTree as ET


class PackageInfo:
    def __init__(self):
        self.id = "未知"
        self.authors = "未知"
        self.company = "未知"
        self.title = "未知"
        self.description = "未知"
        self.copyright = "未知"
        self.project_url = "未知"
        self.repository_url = "未知"
        self.tags = "未知"
        self.license = "未知"


async def get_current_version(csproj_path: str) -> Tuple[str, bool]:
    """获取当前版本号，返回版本号和是否成功"""
    try:
        # 检查文件是否存在
        if not os.path.exists(csproj_path):
            print(f"错误: 找不到项目文件: {csproj_path}")
            return "未知", False
            
        # 使用 UTF-8 编码读取文件内容
        with open(csproj_path, 'r', encoding='utf-8') as file:
            content = file.read()

        # 使用正则表达式提取版本号
        match = re.search(r'<Version>(.*?)</Version>', content)
        if match:
            return match.group(1), True
        else:
            print(f"警告: 在项目文件 {csproj_path} 中找不到版本号")
            return "未知", False
    except Exception as ex:
        print(f"读取当前版本号时发生错误: {ex}")

    return "未知", False


async def get_package_info(csproj_path: str) -> PackageInfo:
    """获取NuGet包的所有信息"""
    try:
        # 使用 UTF-8 编码读取文件内容
        with open(csproj_path, 'r', encoding='utf-8') as file:
            content = file.read()

        # 提取所有包信息
        package_info = PackageInfo()

        id_match = re.search(r'<PackageId>(.*?)</PackageId>', content)
        if id_match:
            package_info.id = id_match.group(1)

        authors_match = re.search(r'<Authors>(.*?)</Authors>', content)
        if authors_match:
            package_info.authors = authors_match.group(1)

        company_match = re.search(r'<Company>(.*?)</Company>', content)
        if company_match:
            package_info.company = company_match.group(1)

        title_match = re.search(r'<Title>(.*?)</Title>', content)
        if title_match:
            package_info.title = title_match.group(1)

        description_match = re.search(r'<Description>(.*?)</Description>', content)
        if description_match:
            package_info.description = description_match.group(1)

        copyright_match = re.search(r'<Copyright>(.*?)</Copyright>', content)
        if copyright_match:
            package_info.copyright = copyright_match.group(1)

        project_url_match = re.search(r'<PackageProjectUrl>(.*?)</PackageProjectUrl>', content)
        if project_url_match:
            package_info.project_url = project_url_match.group(1)

        repository_url_match = re.search(r'<RepositoryUrl>(.*?)</RepositoryUrl>', content)
        if repository_url_match:
            package_info.repository_url = repository_url_match.group(1)

        tags_match = re.search(r'<PackageTags>(.*?)</PackageTags>', content)
        if tags_match:
            package_info.tags = tags_match.group(1)

        license_match = re.search(r'<PackageLicenseExpression>(.*?)</PackageLicenseExpression>', content)
        if license_match:
            package_info.license = license_match.group(1)

        return package_info
    except Exception as ex:
        print(f"读取包信息时发生错误: {ex}")
        return PackageInfo()


async def update_csproj_file(csproj_path: str, version: str, release_notes: str) -> None:
    """更新项目文件"""
    print("正在更新项目文件...")

    # 使用 UTF-8 编码读取文件内容
    with open(csproj_path, 'r', encoding='utf-8') as file:
        content = file.read()

    # 更新版本号
    content = re.sub(r'<Version>.*</Version>', f'<Version>{version}</Version>', content)

    # 更新发布说明
    content = re.sub(r'<PackageReleaseNotes>.*</PackageReleaseNotes>', 
                    f'<PackageReleaseNotes>{release_notes}</PackageReleaseNotes>', content)

    # 使用 UTF-8 编码写回文件
    with open(csproj_path, 'w', encoding='utf-8') as file:
        file.write(content)

    print("项目文件更新完成。")


async def run_command(command: str, arguments: str, working_directory: str = ".") -> int:
    """运行命令并返回退出码"""
    try:
        # 创建子进程
        process = await asyncio.create_subprocess_exec(
            command,
            *arguments.split(),
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
            cwd=os.path.abspath(working_directory)
        )

        # 获取输出
        stdout, stderr = await process.communicate()

        # 输出命令结果（仅在有输出时）
        if stdout:
            print(stdout.decode('utf-8'))

        if stderr:
            print(f"错误: {stderr.decode('utf-8')}")

        return process.returncode
    except Exception as ex:
        print(f"运行命令时发生错误: {ex}")
        return 1


async def build_test_pack_and_publish(version: str) -> int:
    """执行构建、测试、打包和发布流程"""
    print("开始执行构建、测试、打包和发布流程...")

    # 设置工作目录为解决方案根目录
    solution_root = "."

    # 1. 清理之前的构建
    print("1. 清理之前的构建...")
    if await run_command("dotnet", "clean --configuration Release", solution_root) != 0:
        print("清理失败!")
        return 1

    # 2. 恢复依赖项
    print("2. 恢复依赖项...")
    if await run_command("dotnet", "restore", solution_root) != 0:
        print("依赖项恢复失败!")
        return 1

    # 3. 运行测试
    print("3. 运行测试...")
    if await run_command("dotnet", "test", solution_root) != 0:
        print("测试失败!")
        return 1

    # 4. 构建项目
    print("4. 构建项目...")
    if await run_command("dotnet", "build ModbusLib/ModbusLib.csproj --configuration Release", solution_root) != 0:
        print("构建失败!")
        return 1

    # 5. 打包项目
    print("5. 打包项目...")
    if await run_command("dotnet", "pack ModbusLib/ModbusLib.csproj --configuration Release --output nupkg --no-build", solution_root) != 0:
        print("打包失败!")
        return 1

    # 6. 查找生成的包文件
    print("6. 查找生成的包文件...")
    nupkg_directory = os.path.join(solution_root, "nupkg")
    if not os.path.exists(nupkg_directory):
        print("错误: 未找到 nupkg 目录。")
        return 1

    # 查找 .nupkg 文件（排除 .snupkg 文件）
    package_files = [f for f in os.listdir(nupkg_directory) 
                    if f.endswith('.nupkg') and not f.endswith('.snupkg')]

    if not package_files:
        print("错误: 未找到生成的 NuGet 包文件。")
        return 1

    package_file = os.path.join(nupkg_directory, package_files[0])
    print(f"找到主包文件: {package_file}")

    # 查找 .snupkg symbol 包文件
    symbol_package_files = [f for f in os.listdir(nupkg_directory) if f.endswith('.snupkg')]

    symbol_package_file = None
    if symbol_package_files:
        symbol_package_file = os.path.join(nupkg_directory, symbol_package_files[0])
        print(f"找到符号包文件: {symbol_package_file}")
    else:
        print("警告: 未找到生成的符号包文件。")

    # 7. 发布到 NuGet
    print("7. 发布到 NuGet...")
    nuget_source = "https://api.nuget.org/v3/index.json"
    api_key = os.environ.get("NUGET_API_KEY", "")

    if not api_key:
        print("错误: 未设置 NuGet API 密钥环境变量 NUGET_API_KEY。")
        return 1

    # 发布主包
    print("发布主包...")
    result = await run_command("dotnet", f"nuget push \"{package_file}\" --api-key {api_key} --source {nuget_source}", solution_root)

    if result != 0:
        print("NuGet 主包发布失败!")
        return result

    print(f"NuGet 主包 {version} 发布成功!")

    # 如果存在符号包，则发布符号包
    if symbol_package_file:
        print("发布符号包...")
        result = await run_command("dotnet", f"nuget push \"{symbol_package_file}\" --api-key {api_key} --source {nuget_source}", solution_root)

        if result != 0:
            print("NuGet 符号包发布失败!")
            return result

        print(f"NuGet 符号包 {version} 发布成功!")

    return result


async def main():
    """主函数"""
    # 获取用户输入的版本号和版本描述
    print("=== ModbusLib NuGet 包发布工具 ===")

    # 先读取当前版本号
    csproj_path = "ModbusLib/ModbusLib.csproj"
    current_version, success = await get_current_version(csproj_path)
    print(f"当前版本号: {current_version}")
    
    # 如果读取版本号失败，退出程序
    if not success:
        print("无法读取当前版本号，程序退出。")
        return 1
        
    version = input("请输入新版本号: ")

    if not version.strip():
        print("错误: 版本号不能为空。")
        return 1

    release_notes = input("请输入版本描述信息: ")

    if not release_notes.strip():
        print("错误: 版本描述信息不能为空。")
        return 1

    # 显示发布预览信息
    print("\n==== 发布预览 ====")
    # 获取并显示NuGet包的所有信息
    package_info = await get_package_info(csproj_path)
    print(f"包ID: {package_info.id}")
    print(f"版本: {version}")
    print(f"作者: {package_info.authors}")
    print(f"公司: {package_info.company}")
    print(f"标题: {package_info.title}")
    print(f"描述: {package_info.description}")
    print(f"版权: {package_info.copyright}")
    print(f"项目URL: {package_info.project_url}")
    print(f"仓库URL: {package_info.repository_url}")
    print(f"标签: {package_info.tags}")
    print(f"许可证: {package_info.license}")
    print(f"版本描述: {release_notes}")

    # 显示将要发布的文件路径
    solution_root = "."
    nupkg_directory = os.path.join(solution_root, "nupkg")
    package_file_name = f"Himmelt.ModbusLib.{version}.nupkg"
    symbol_package_file_name = f"Himmelt.ModbusLib.{version}.snupkg"
    package_file_path = os.path.join(nupkg_directory, package_file_name)
    symbol_package_file_path = os.path.join(nupkg_directory, symbol_package_file_name)
    print(f"主包文件路径: {package_file_path}")
    print(f"符号包文件路径: {symbol_package_file_path}")

    print("==================")

    confirm = input("确认发布? (y/N): ")

    if confirm.lower() != 'y':
        print("发布已取消。")
        return 0

    try:
        # 更新 ModbusLib.csproj 文件
        await update_csproj_file(csproj_path, version, release_notes)

        # 执行构建、测试、打包、发布
        result = await build_test_pack_and_publish(version)

        if result == 0:
            print("\nNuGet 包发布成功!")
            return 0
        else:
            print("\nNuGet 包发布失败!")
            return result
    except Exception as ex:
        print(f"发生错误: {ex}")
        return 1


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))