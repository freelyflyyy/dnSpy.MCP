<div align="center">

# dnSpy.MCP

[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Protocol](https://img.shields.io/badge/MCP-Code%20Mode-black?style=flat-square)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/License-GPLv3-blue?style=flat-square)](LICENSE.txt)

[**English**](README.md) | [简体中文](README_CN.md)

</div>

---

## Project Overview

**dnSpy.MCP** uses a **Code Mode** architecture to integrate [**dnSpyEx**](https://github.com/dnSpyEx/dnSpy) with **MCP**.\
It moves beyond rigid JSON interfaces that map each feature to a separate tool. By embedding the Roslyn C# scripting engine (`Microsoft.CodeAnalysis.CSharp.Scripting`) inside the dnSpy process and communicating with the MCP server over named pipes, it lets large language models write and execute `.csx` scripts directly inside a running dnSpy instance for flexible .NET reverse engineering and code analysis.

## Features and Advantages

The project provides only three basic tools:

| Tool | Purpose |
|---|---|
| `session` | Discover candidate dnSpy named pipes matching the naming convention |
| `search_api` | Look up scripting API signatures, descriptions, and examples |
| `execute_csharp` | Execute C# scripts inside a target dnSpy instance |

Traditional MCP plugins for reverse engineering typically rely on a fixed set of predefined tools, such as `list_assemblies`, `get_types`, and `decompile_method`. This can become a bottleneck in complex analysis tasks. **dnSpy.MCP** takes a different approach with **Code Mode**:

* **Unrestricted expressiveness**
  AI is no longer limited to predefined tool parameters. By executing C# scripts directly inside dnSpy, the model can combine LINQ queries, conditional branches, filtering loops, and tracing across assemblies to carry out complex reverse-engineering workflows in a single execution.
* **Efficient interaction**
  With conventional tools, locating specific logic in a large assembly can require a dozen or more tool calls. Large intermediate lists quickly consume tokens and clutter the model's context. With Code Mode, traversal, filtering, and aggregation all happen inside dnSpy, and only the final, relevant results are returned to the model.
* **Built-in API reference**
  To help prevent the model from inventing APIs, the server includes an `ApiCatalog`, exposed through the `search_api` tool. Before writing scripts, AI can look up the exact method signatures and usage examples available through the global `ctx` context, helping avoid hallucinated API calls.

## Requirements

| Component | Requirement |
| :--- | :--- |
| Operating system | Windows |
| dnSpy | dnSpyEx 6.6.0, .NET 10 or .NET Framework 4.8; choose the matching Windows x64 package. |
| Runtime | The packaged Server is self-contained. Framework dnSpy still requires .NET Framework 4.8. |
| MCP client | Support for local `stdio` servers, such as Claude Code or Codex |

The requirements above are sufficient to use a release package. Building from source also requires the **.NET 10 SDK**.

## Learn More

To learn more about the project, see:

* [**Installation and Configuration Guide**](docs/Install.md) — Environment requirements, building from source, installing the dnSpy extension, and configuring MCP clients.
* [**Architecture and Implementation Details**](docs/Architecture.md) — Named-pipe IPC, the Roslyn scripting environment, the exposed internal APIs, and `.csx` script examples.

## Security Notice

This project allows arbitrary C# code to be dynamically compiled and executed inside the dnSpy process through local named pipes (restricted to the current user with `CurrentUserOnly`). Use it only in trusted local development environments with trusted MCP clients.

## License

This project is open source under the GPLv3 license. See [LICENSE](LICENSE.txt) for details.
