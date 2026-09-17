# AGENTS.md

本文件面向在本仓库工作的 AI 编码助手与维护者，说明**构建、测试与 NuGet 发布流程**。
发布相关规则以 `.github/workflows/ci.yml` 为准；若 workflow 有变，请同步更新本文件。

---

## 仓库结构速览

| 路径 | 说明 |
| --- | --- |
| `ModbusLib/` | 库本体，多目标框架 `net8.0;net9.0;net10.0` |
| `ModbusLib.Tests/` | xUnit v3 测试（`TestContext.Current.CancellationToken`） |
| `ModbusLib.slnx` | 解决方案（CI 用它 restore/build/test） |
| `temp/` | 临时脚手架（已 gitignore），验证用小程序放这里 |
| `nupkg/`、`artifacts/` | 本地打包输出（已 gitignore） |

## 构建与测试

```bash
dotnet build ModbusLib.slnx -c Release

# 单个框架
dotnet test ModbusLib.Tests/ModbusLib.Tests.csproj -f net10.0

# 发布前建议三个框架全跑
for t in net8.0 net9.0 net10.0; do dotnet test ModbusLib.Tests/ModbusLib.Tests.csproj -f $t; done
```

**发布门槛**：Release 构建 0 警告 0 错误，且三个目标框架全部测试通过。

---

## NuGet 发布流程

### 一、触发规则（唯一依据：`.github/workflows/ci.yml`）

- 触发条件：`on.push` 监听 `branches: [main]` 与 `tags: ["v*"]`。
- `build-test` job（windows-latest）：`restore` → `build -c Release` → `test -c Release` → `pack` → 上传 artifact `nuget-packages`。
- `publish` job（ubuntu-latest）：带 `if: startsWith(github.ref, 'refs/tags/v')` 且 `needs: build-test`，执行 `dotnet nuget push` 到 nuget.org。

| 推送内容 | 构建/测试/打包 | 发布到 nuget.org |
| --- | --- | --- |
| 推 `main` | ✅ | ❌ **不会发布** |
| 推 `v*` tag（如 `v1.0.5`） | ✅ | ✅ `build-test` 成功后执行 |

> **结论：发布必须打 `v*` tag，单纯推分支不触发发布。**
> tag 需最终同步到 GitHub 仓库（Actions 只在 GitHub 上运行）。本仓库存在多个远端，按现有同步机制推送即可，不要求直推 GitHub——但必须确认 tag 确实同步过去了。

### 二、关键约束（易踩坑）

1. **版本号唯一来源是 `ModbusLib/ModbusLib.csproj` 的 `<Version>`，与 tag 名无关。**
   tag 名不参与打包，两者必须一致：tag `v1.0.5` ↔ `<Version>1.0.5</Version>`。
   若不一致，推上去的是 csproj 里的版本（例如 tag 是 v1.0.5、csproj 还是 1.0.4，实际发布的是 1.0.4）。
2. 需要仓库 secret **`NUGET_API_KEY`**。

### 三、标准发布步骤

1. 更新 `ModbusLib/ModbusLib.csproj`：
   - `<Version>` 提升为 `X.Y.Z`
   - `<PackageReleaseNotes>` 追加一条，格式与历史一致：`vX.Y.Z: <本次变更摘要>`；
     **公开行为变更（语义变化、异常形态变化、API 新增/变更）必须写明**。
2. 本地验证打包产物与版本号：

   ```bash
   dotnet pack ModbusLib/ModbusLib.csproj -c Release -o temp/pack-check
   ls temp/pack-check      # 期望：Himmelt.ModbusLib.X.Y.Z.nupkg 与 .snupkg
   ```

   需要时确认包内版本：`unzip -p temp/pack-check/Himmelt.ModbusLib.X.Y.Z.nupkg "*.nuspec" | grep version`

3. 提交。历史惯例为「修复/功能提交」与「版本提交」分开：
   `chore(release): 升级版本号至 X.Y.Z`
4. 打附注 tag 并推送（tag 风格与历史一致，如 `v1.0.4` 为附注 tag）：

   ```bash
   git tag -a vX.Y.Z -m "vX.Y.Z"
   git push <常用远端> main
   git push <常用远端> vX.Y.Z
   ```
