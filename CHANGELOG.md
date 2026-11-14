# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-11-15

### Added
- **Dual Runtime Support**: Support both dnSpyEx .NET 10 (`net10`) and .NET Framework 4.8 (`net48`) runtimes.
- **Unified Logging Architecture**: Introduced `dnSpy.MCP.Logging` to standardize log sinks, text formatters, and lifecycle management across both dnSpy extension and the MCP server.
- **Session Handshake and Discovery**: Added session handshake verification, process ID validation, and multi-instance discovery over named pipes.
- **Installation and Verification Scripts**: Added `scripts/install.ps1`, `scripts/install.cmd`, and `scripts/test-package.ps1` for automated setup, smoke testing, and package verification.
- **UI Menu Enhancements**: Added dedicated "Service Status" and "Run Script" commands to dnSpy's MCP menu.

### Changed
- **Protocol Hardening**: Refactored named pipe communication with structured response envelopes (`PipeResponse<T>`), explicit protocol version checks, and enhanced error handling.
- **Extension Menu Layout**: Reorganized menu entries and improved output view routing.

## [0.1.0] - 2026-11-01

### Added
- Initial release of dnSpy.MCP.
- Embedded Roslyn C# scripting engine (`Microsoft.CodeAnalysis.CSharp.Scripting`) inside dnSpyEx.
- MCP Server providing `session`, `search_api`, and `execute_csharp` tools over `stdio`.
- Basic named pipe communication between dnSpy and MCP Server.
- Automated Windows CI packaging and release pipeline.
