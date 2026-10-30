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

## Introduction
**dnSpy.MCP** deeply integrates [**dnSpyEx**](https://github.com/dnSpyEx/dnSpy) with the **Model Context Protocol (MCP)** using a **Code Mode** architecture.

Moving away from rigid, traditional "one-tool-per-feature" JSON wrappers, this project embeds the Roslyn C# scripting engine (`Microsoft.CodeAnalysis.CSharp.Scripting`) directly inside the dnSpy process and communicates with the MCP server via Named Pipes. This enables Large Language Models (LLMs) to write and execute `.csx` scripts directly within a running dnSpy instance, unlocking highly flexible .NET reverse engineering and code analysis.

## Features & Advantages

The project exposes only three core tools:

| Tool | Description |
|---|---|
| `session` | Discover candidate dnSpy named pipes matching the naming convention |
| `search_api` | Query scripting API signatures, documentation, and usage examples |
| `execute_csharp` | Execute C# scripts inside a target dnSpy instance |

Traditional reverse-engineering MCP plugins rely on predefined, static toolsets (such as `list_assemblies`, `get_types`, or `decompile_method`), which quickly hit bottlenecks during complex analysis. **dnSpy.MCP**'s **Code Mode** takes a different approach:

- **Unlimited Expressiveness**  
  The AI is no longer constrained by hardcoded tool parameters. By executing C# scripts directly inside the dnSpy process, the model can freely combine LINQ queries, conditional branching, loop filtering, and cross-assembly tracing to perform arbitrarily complex reverse-engineering workflows in a single execution.
- **Maximum Interaction Efficiency**  
  With traditional tool-calling, locating specific logic inside a large assembly often requires a dozen round-trips, flooding and polluting the context window with intermediate lists. In Code Mode, all traversal, filtering, and aggregation happen in-process inside dnSpy—returning only the exact, final results to the model.
- **Built-in API Catalog**  
  To prevent the model from hallucinating non-existent APIs, the server includes a built-in `ApiCatalog` (exposed via the `search_api` tool). Before writing code, the AI can look up exact method signatures and usage examples exposed by the global `dnspy` context on demand.

## Learn More

If you are interested in this project and want to dive deeper, check out:

- [**Installation & Setup**](docs/Install.md) — Prerequisites, building from source, extension installation, and MCP client configuration.
- [**Architecture & Implementation**](docs/Architecture.md) — Named Pipe IPC protocol, Roslyn execution environment, exposed API surface, and .csx script examples.

## Security Notice

This project compiles and executes arbitrary C# scripts inside the running dnSpy process over local Named Pipes (restricted to CurrentUserOnly). Use it only in trusted local environments with trusted MCP clients.

## License

This project is licensed under the GPLv3 License. See [LICENSE](LICENSE.txt) for details.