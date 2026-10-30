<div align="center">

# 安装与配置

[English](Install.md) | [**简体中文**](Install_CN.md)

</div>

---

本文说明如何从源码编译 `dnSpy.MCP`、手动部署插件与 Server，以及接入 MCP 客户端。下文以 `D:\Tools\dnSpyEx` 作为 dnSpy 安装路径示例，操作时请替换为你的实际路径。

## 环境要求

| 组件 | 要求 |
| :--- | :--- |
| 操作系统 | Windows |
| dnSpy 版本 | dnSpyEx `.NET 10` 构建版（已在 6.6.0 验证；不支持 `.NET Framework` 版） |
| 编译环境 | .NET 10 SDK（需包含 Windows 桌面开发组件） |
| Server 运行时 | 系统全局安装的 .NET 10 Runtime |
| MCP 客户端 | 支持本地 `stdio` 传输的客户端 |

> **注意：** 插件目标框架为 `net10.0-windows`。另外，dnSpyEx 自带的运行时仅供其自身使用，独立的 MCP Server 需要系统安装的 .NET 10 Runtime 才能启动。

---

## 1. 源码编译

克隆或下载仓库后，在仓库根目录打开 PowerShell 运行构建脚本：

```powershell
.\scripts\build.ps1 -BuildId manual-001 -DnSpyDir 'D:\Tools\dnSpyEx\bin'
```

* `-DnSpyDir` 必须指向包含 `dnSpy.Contracts.DnSpy.dll` 的 `bin` 目录，而不是外层存放 `dnSpy.exe` 的根目录。
* `-BuildId` 可选。脚本不会覆盖已有的同名输出目录；省略该参数会自动按时间戳生成目录名，重新构建时也可以指定新的编号。

脚本会以 `Release` 模式编译整个解决方案，并将产物整理到 `artifacts/` 下：

```text
artifacts/build-manual-001/
├─ extension/
└─ server/
```

---

## 2. 部署插件与 Server

复制文件前，请先关闭 dnSpy，并断开正在使用旧版 Server 的 MCP 客户端。

1. 将 `artifacts/build-manual-001/extension/` 下的所有文件复制到 `D:\Tools\dnSpyEx\bin\Extensions\`（若没有 `Extensions` 文件夹请手动新建）。
2. 将 `artifacts/build-manual-001/server/` 整个文件夹复制到 `D:\Tools\dnSpyEx\server\`。

部署完成后的目录结构如下：

```text
dnSpyEx/
├─ dnSpy.exe
├─ bin/
│  └─ Extensions/
│     ├─ dnSpy.MCP.Extension.x.dll
│     ├─ dnSpy.MCP.Extension.x.deps.json
│     └─ dnSpy.MCP.Protocol.dll
└─ server/
   ├─ dnSpy.MCP.Server.dll
   ├─ dnSpy.MCP.Server.deps.json
   ├─ dnSpy.MCP.Server.runtimeconfig.json
   ├─ dnSpy.MCP.Protocol.dll
   └─ 其他发布依赖与子目录
```

> **版本更新提示：** 升级时请务必使用同一次构建产出的文件，同时替换插件、`dnSpy.MCP.Protocol.dll` 和整个 `server/` 目录。Server 必须保留完整的发布目录，单独复制一个 `dnSpy.MCP.Server.dll` 无法运行。

启动 `dnSpy.exe`，随便拖入一个 .NET 程序集，然后在顶部菜单栏点击 **视图 → 打开/关闭管道服务**。此时底部输出窗口会打印出当前开启的管道名称：

```text
dnSpy-Mcp-12345
```

末尾的数字是当前 dnSpy 的进程号（PID），每次重启 dnSpy 后都会变化。

---

## 3. 配置 MCP 客户端

在客户端中按以下参数添加 MCP 服务：

| 配置项 | 内容 |
| :--- | :--- |
| 服务名称 | `dnspy` |
| 传输方式 | `stdio` |
| 启动命令 | `C:\Program Files\dotnet\dotnet.exe` |
| 启动参数 | `D:\Tools\dnSpyEx\server\dnSpy.MCP.Server.dll` |

单服务 JSON 配置示例：

```json
{
  "type": "stdio",
  "command": "C:\\Program Files\\dotnet\\dotnet.exe",
  "args": [
    "D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"
  ]
}
```

如果使用 CC Switch 管理配置，把上面这段 JSON 填进单个 MCP 服务的编辑框，再打开对应客户端的同步开关即可。

这里传入的 DLL 路径是给 `dotnet` 命令使用的，Server 本身不接收命令行启动参数；目标进程 PID 和要跑的脚本都是在后续工具调用时动态传入的。另外，请确保 MCP 客户端与 dnSpy 运行在同一个 Windows 账户及相同的权限级别下（例如同为普通权限，或同为管理员权限）。

---

## 4. 验证连接

保存配置并刷新客户端连接后，正常情况下会加载出三个工具：

| 工具名 | 用途 |
| :--- | :--- |
| `search_api` | 检索内置的脚本 API 文档（文档内容为英文，请用接口名或英文关键词搜索，如 `ListAssemblies`） |
| `session` | 获取当前开启了管道服务的 dnSpy 候选进程列表 |
| `execute_csharp` | 在指定的 dnSpy 进程内执行 C# 脚本 |

可以先调一次 `execute_csharp` 做连通性测试（把 `12345` 换成 `session` 查出来的实际 PID）：

```json
{
  "targetProcessId": 12345,
  "code": "return 1 + 2;"
}
```

正常返回如下：

```json
{"Success":true,"Value":3,"Error":null}
```

接着可以执行 `return ctx.Documents.ListAssemblies();` 测试能否读到当前打开的程序集列表（若 dnSpy 里没加载任何文件，返回空数组 `[]` 也是正常的）。如果本地同时开了多个 dnSpy 窗口，执行脚本前请先核对目标 PID。

---

## 常见问题排查

| 现象 | 排查方向 |
| :--- | :--- |
| Server 启动失败 | 检查 `dotnet.exe` 路径是否正确、系统是否装了 .NET 10 Runtime，以及 `server/` 目录文件是否齐全。 |
| dnSpy 里找不到菜单项 | 确认使用的是 `.NET 10` 版 dnSpyEx，检查 `bin\Extensions\` 下是否漏了 `Protocol.dll`，并重启 dnSpy。 |
| `session` 返回 `[]` | 确认 dnSpy 已启动，且点击了 **视图 → 打开/关闭管道服务**。 |
| 能看到候选 PID 但执行报错 | 检查该 dnSpy 进程是否已经关闭，或者客户端与 dnSpy 是否存在权限不一致（一个管理员、一个普通权限）。 |
| 脚本执行超时 | 超时只会中断客户端侧的等待，已经发过去的脚本可能仍在 dnSpy 内部运行。切勿对包含修改或副作用的脚本做盲目重试。 |