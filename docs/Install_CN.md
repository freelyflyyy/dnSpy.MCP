<div align="center">

# 安装与配置

[English](Install.md) | [**简体中文**](Install_CN.md)

</div>

## Quick Start

以下以 Windows x64、dnSpy 安装在 `D:\Tools\dnSpyEx` 为例，请按实际情况修改路径。

### 1. 准备环境

- 下载 [dnSpyEx 6.6.0](https://github.com/dnSpyEx/dnSpy/releases/tag/v6.6.0) 的 `dnSpy-net-win64.zip`，解压到本地。当前插件适配 .NET 10 版，不支持 .NET Framework 版。
- 安装 [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 的 Windows x64 版本。已有 .NET 10 SDK 或 Desktop Runtime 可跳过。

### 2. 安装插件

从 [Releases](https://github.com/freelyflyyy/dnSpy.MCP/releases) 下载 `dnSpy-MCP-v<版本>-windows.zip` 并解压。

关闭 dnSpy 和正在使用该服务的 MCP 客户端，然后复制：

| 发布包内容 | 安装位置 |
|---|---|
| `extension/` 内的全部文件 | `D:\Tools\dnSpyEx\bin\Extensions\` |
| 整个 `server/` 文件夹 | `D:\Tools\dnSpyEx\server\` |

没有 `Extensions` 文件夹就新建。`server/` 中的依赖文件和子目录需要完整保留。

### 3. 开始使用

1. 启动 dnSpy，打开要分析的 .NET 程序集。
2. 点击 **视图 → 打开/关闭管道服务**。
3. 按下方说明配置 MCP 客户端，保存后重启客户端。

连接后，可以直接对 AI 说：

> 使用 dnspy 列出当前加载的程序集。

每次重启 dnSpy 后，需要重新打开管道服务。

## 客户端配置

选择自己使用的一种方式即可。替换示例中的 dnSpy 路径；如果 `dotnet.exe` 安装在其他位置，也需要修改 `command`。

已有配置时，添加或更新 `dnspy` 条目，保留其他服务。

<details>
<summary>Codex</summary>

在 `%USERPROFILE%\.codex\config.toml` 中添加：

```toml
[mcp_servers.dnspy]
command = "C:\\Program Files\\dotnet\\dotnet.exe"
args = ["D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"]
cwd = "D:\\Tools\\dnSpyEx\\server"
```

</details>

<details>
<summary>Claude Code</summary>

在你使用 Claude Code 的项目根目录创建或编辑 `.mcp.json`：

```json
{
  "mcpServers": {
    "dnspy": {
      "type": "stdio",
      "command": "C:\\Program Files\\dotnet\\dotnet.exe",
      "args": ["D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"]
    }
  }
}
```

在该项目中启动 Claude Code，按提示启用此 MCP 服务。

</details>

<details>
<summary>CC Switch</summary>

打开 MCP 管理，新增名为 `dnspy` 的服务，在单个服务的 JSON 编辑框中填入：

```json
{
  "type": "stdio",
  "command": "C:\\Program Files\\dotnet\\dotnet.exe",
  "args": ["D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"],
  "cwd": "D:\\Tools\\dnSpyEx\\server"
}
```

保存并启用目标客户端的同步开关，然后重启对应客户端。

</details>

## 从源码安装

需要 [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)。

克隆或下载本仓库，在仓库根目录打开 PowerShell。关闭 dnSpy 和正在使用该服务的客户端，然后执行：

```powershell
.\scripts\install.ps1 -DnSpyDir 'D:\Tools\dnSpyEx'
```

脚本会完成编译和部署，并生成、打开 Claude Code、Codex 和 CC Switch 的配置文件。将对应内容加入客户端配置，再按上面的步骤启动管道服务。

这里传入的是 **dnSpy 安装根目录**，即包含 `dnSpy.exe` 的文件夹。

如果只需要构建发布包：

```powershell
.\scripts\build.ps1 -DnSpyDir 'D:\Tools\dnSpyEx\bin'
```

构建脚本传入的是 **`bin` 目录**。产物位于脚本输出的 `artifacts/build-<编号>/`，按 Quick Start 中的位置复制即可。

## 更新版本

关闭 dnSpy 和正在使用该服务的客户端，备份旧文件后，用同一个版本包更新插件文件，并整体替换 `server/`。保留其他插件。

从源码运行安装脚本时，旧文件会自动备份到：

```text
<dnSpy安装目录>\.dnspy-mcp-backups\install-<编号>\
```

## 常见问题

| 问题 | 处理方法 |
|---|---|
| 客户端无法启动服务 | 检查配置中的路径、.NET 10 Runtime，以及 `server/` 是否复制完整。 |
| dnSpy 没有管道服务菜单 | 确认使用 .NET 10 版 dnSpyEx，文件位于 `bin\Extensions\`，然后重启 dnSpy。 |
| AI 找不到 dnSpy 会话 | 打开管道服务；让 dnSpy 和 MCP 客户端使用同一个 Windows 账户及相同权限级别运行。 |