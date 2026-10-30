<div align="center">

# dnSpy.MCP

[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/zh-cn/dotnet/csharp/)
[![Protocol](https://img.shields.io/badge/MCP-Code%20Mode-black?style=flat-square)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/License-GPLv3-blue?style=flat-square)](LICENSE.txt)

[English](README.md) | [**简体中文**](README_CN.md)

</div>

---

## 项目简介

&emsp;&emsp;**dnSpy.MCP** 采用 **Code Mode（代码模式）** 架构将[**dnSpyEx**](https://github.com/dnSpyEx/dnSpy) 与 **MCP** 深度集成。\
&emsp;&emsp;本项目摒弃了传统的“一功能一工具”的死板 JSON 接口封装，通过在 dnSpy 进程内嵌 Roslyn C# 脚本引擎（`Microsoft.CodeAnalysis.CSharp.Scripting`），
并利用命名管道（Named Pipes）与 MCP 服务端通信，允许大语言模型直接在正在运行的 dnSpy 进程内部编写并执行 `.csx` 脚本，实现高度灵活的 .NET 逆向工程与代码分析。

## 功能与优势

项目只提供三个基础工具：

| 工具 | 用途 |
|---|---|
| `session` | 发现符合命名规则的候选 dnSpy 管道 |
| `search_api` | 查询脚本 API 的签名、说明和示例 |
| `execute_csharp` | 在指定 dnSpy 实例中执行 C# 脚本 |

&emsp;&emsp;传统的逆向工程 MCP 插件通常依赖预先定义好的固定工具集（例如 `list_assemblies`、`get_types`、`decompile_method`），在面对复杂分析场景时极易遇到瓶颈。
**dnSpy.MCP** 采用的 **Code Mode** 则不同：

* **表达能力无上限**
  AI 不再受限于预先写死的工具参数。通过在 dnSpy 进程内直接执行 C# 脚本，模型可以自由组合 LINQ 查询、条件分支、循环过滤以及跨程序集追踪，
  在单次执行中完成任意复杂度的逆向分析逻辑。
* **极致的交互效率**
  在传统工具模式下，从大型程序集中定位特定逻辑往往需要十几次工具往返调用，大量中间列表会迅速消耗并污染 Token 上下文。在 Code Mode 下，
  所有遍历、筛选与聚合计算全部在 dnSpy 进程内完成，仅向模型返回最终的精准结果。
* **内置 API 字典**
  为防止模型编造不存在的接口，服务端内置了 `ApiCatalog`（提供`search_api` 工具）。AI 在编写代码前可按需查阅全局 `dnspy` 上下文暴露的精确方法签名与调用示例，
  有效消除模型幻觉。


## 深入了解

如果您对本项目感兴趣，并且想进一步了解，请移步：

* [**安装与配置指南**](docs/Install_CN.md) — 环境要求、源码编译、dnSpy 插件安装以及 MCP 客户端配置方法。
* [**架构与实现细节**](docs/Architecture_CN.md) — 命名管道 IPC 协议、Roslyn 脚本执行环境、内部 API 暴露结构以及 `.csx` 脚本示例。


## 安全说明

&emsp;&emsp;本项目允许通过本地命名管道（仅限当前用户 `CurrentUserOnly`）在 dnSpy 进程内动态编译并执行任意 C# 代码。请仅在受信任的本地开发环境和受信任的 MCP 客户端中使用。

## 许可证

本项目基于 GPLv3 许可证开源，详情请参阅 [LICENSE](LICENSE.txt)。