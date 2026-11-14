<div align="center">

# 安装与配置

[English](Install.md) | [**简体中文**](Install_CN.md)

</div>

## Quick Start

以下以 Windows x64、dnSpy 安装在 `D:\Tools\dnSpyEx` 为例，请按实际情况修改路径。

### 1. 准备环境

- 下载 [dnSpyEx 6.6.0](https://github.com/dnSpyEx/dnSpy/releases/tag/v6.6.0) 的 .NET 10 或 .NET Framework 4.8 版本，解压到本地。
- 发布包内的 Server 自带运行时，无需安装 Visual Studio、.NET SDK 或额外的 .NET 10 Runtime。Framework 版 dnSpy 本身仍需 .NET Framework 4.8。

### 2. 安装插件

从 [Releases](https://github.com/freelyflyyy/dnSpy.MCP/releases) 下载 **`dnSpy-MCP-v<版本>-windows.zip`** 并解压；Framework 版 dnSpy 选择 **`dnSpy-MCP-v<版本>-net48-windows.zip`**。

关闭 dnSpy 和正在使用该服务的 MCP 客户端，双击 `install.cmd`，输入 dnSpy 安装目录或 `dnSpy.exe` 的完整路径。

| 发布包内容 | 安装位置 |
|---|---|
| Server EXE、插件 DLL 和必要依赖 | `D:\Tools\dnSpyEx\bin\Extensions\` |

安装脚本会校验发布包、创建目标目录并生成客户端配置。运行文件统一放在一个目录中，不再区分 `extension/` 和 `server/`。

### 3. 开始使用

1. 启动 dnSpy，打开要分析的 .NET 程序集。
2. 管道服务会自动启动，可在 **MCP** 菜单中查看状态。
3. 按下方说明配置 MCP 客户端，保存后重启客户端。

连接后，可以直接对 AI 说：

> 使用 dnspy 列出当前加载的程序集。

如果手动停止了服务，可通过 **MCP → Start Service** 重新启动。

## 客户端配置

选择自己使用的一种方式即可，替换示例中的 dnSpy 路径。安装脚本也会在 `bin\Extensions\` 中生成对应的配置文件。

已有配置时，添加或更新 `dnspy` 条目，保留其他服务。

<details>
<summary>Codex</summary>

在 `%USERPROFILE%\.codex\config.toml` 中添加：

```toml
[mcp_servers.dnspy]
command = "D:\\Tools\\dnSpyEx\\bin\\Extensions\\dnSpy.MCP.Server.exe"
args = []
cwd = "D:\\Tools\\dnSpyEx\\bin\\Extensions"
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
      "command": "D:\\Tools\\dnSpyEx\\bin\\Extensions\\dnSpy.MCP.Server.exe",
      "args": []
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
  "command": "D:\\Tools\\dnSpyEx\\bin\\Extensions\\dnSpy.MCP.Server.exe",
  "args": [],
  "cwd": "D:\\Tools\\dnSpyEx\\bin\\Extensions"
}
```

保存并启用目标客户端的同步开关，然后重启对应客户端。

</details>

## 从源码安装

需要 [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)。

克隆或下载本仓库，在仓库根目录打开 PowerShell。关闭 dnSpy 和正在使用该服务的客户端，然后执行：

```powershell
.\scripts\build.ps1 -BuildId local-net10 -HostRuntime net10 -DnSpyDir 'D:\Tools\dnSpyEx\bin'
.\scripts\install.ps1 -PackageDir '.\artifacts\build-local-net10' -DnSpyDir 'D:\Tools\dnSpyEx'
```

先构建发布包，再安装并生成 Claude Code、Codex 和 CC Switch 的配置文件。安装脚本本身不调用编译工具，也不会修改客户端设置。

安装脚本传入的是 **dnSpy 安装根目录**，即包含 `dnSpy.exe` 的文件夹。

如果只需要构建 Framework 版发布包：

```powershell
.\scripts\build.ps1 -BuildId local-net48 -HostRuntime net48 -DnSpyDir 'D:\Tools\dnSpyEx-NetFramework\bin'
```

构建脚本传入的是 **`bin` 目录**。产物位于脚本输出的 `artifacts/build-<编号>/`，也可进入该目录运行 `install.cmd` 安装。

## 更新版本

关闭 dnSpy 和正在使用该服务的客户端，解压新版本包并运行 `install.cmd`。安装器会更新本插件的文件并保留其他插件，旧文件自动备份到：

```text
<dnSpy安装目录>\.dnspy-mcp-backups\install-<编号>\
```

旧版 `server/` 目录不会自动删除。先把客户端配置切换到新的 EXE，确认可用后再处理旧目录。

## 常见问题

| 问题 | 处理方法 |
|---|---|
| 客户端无法启动服务 | 检查配置是否指向 `bin\Extensions\dnSpy.MCP.Server.exe`，并确认已使用同一发布包完成安装。 |
| dnSpy 没有管道服务菜单 | 确认发布包与 dnSpy 的运行时匹配，文件位于 `bin\Extensions\`，然后重启 dnSpy。 |
| AI 找不到 dnSpy 会话 | 查看 MCP 菜单中的服务状态；让 dnSpy 和 MCP 客户端使用同一个 Windows 账户及相同权限级别运行。 |
