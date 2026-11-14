<div align="center">

# Installation & Setup

[**English**](Install.md) | [简体中文](Install_CN.md)

</div>

## Quick Start

These examples use Windows x64 with dnSpy installed at `D:\Tools\dnSpyEx`. Adjust the paths for your setup.

### 1. Prepare the environment

- Download the .NET 10 or .NET Framework 4.8 build of [dnSpyEx 6.6.0](https://github.com/dnSpyEx/dnSpy/releases/tag/v6.6.0) and extract it.
- The packaged Server includes its runtime. No Visual Studio, .NET SDK or additional .NET 10 Runtime is needed. The Framework host still requires .NET Framework 4.8.

### 2. Install the extension

Download **`dnSpy-MCP-v<version>-windows.zip`** from [Releases](https://github.com/freelyflyyy/dnSpy.MCP/releases) for the .NET 10 host, or **`dnSpy-MCP-v<version>-net48-windows.zip`** for the Framework host, and extract it.

Close dnSpy and any MCP clients using this server, then run `install.cmd`. Enter the dnSpy installation directory or the full path to `dnSpy.exe`.

| Package contents | Destination |
|---|---|
| Server EXE, extension DLLs and required dependencies | `D:\Tools\dnSpyEx\bin\Extensions\` |

The installer checks the package, creates the target directory and generates client configuration files. All runtime files are in one directory; there are no separate `extension/` and `server/` folders.

### 3. Start using it

1. Launch dnSpy and open the .NET assembly you want to analyze.
2. The pipe service starts automatically. Check its status in the **MCP** menu.
3. Configure your MCP client below, save, and restart the client.

Once connected, ask your AI:

> Use dnspy to list the currently loaded assemblies.

If you stop the service manually, use **MCP → Start Service** to start it again.

## Client Configuration

Choose one setup method and replace the sample dnSpy path. The installer also generates configuration files under `bin\Extensions\`.

If you already have a configuration, add or update the `dnspy` entry while keeping your other servers.

<details>
<summary>Codex</summary>

Add this to `%USERPROFILE%\.codex\config.toml`:

```toml
[mcp_servers.dnspy]
command = "D:\\Tools\\dnSpyEx\\bin\\Extensions\\dnSpy.MCP.Server.exe"
args = []
cwd = "D:\\Tools\\dnSpyEx\\bin\\Extensions"
```

</details>

<details>
<summary>Claude Code</summary>

Create or edit `.mcp.json` in the project where you use Claude Code:

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

Start Claude Code in that project and enable the MCP server when prompted.

</details>

<details>
<summary>CC Switch</summary>

Open MCP management, add a server named `dnspy`, and paste this into the single-server JSON editor:

```json
{
  "type": "stdio",
  "command": "D:\\Tools\\dnSpyEx\\bin\\Extensions\\dnSpy.MCP.Server.exe",
  "args": [],
  "cwd": "D:\\Tools\\dnSpyEx\\bin\\Extensions"
}
```

Save, enable synchronization for your target client, and restart that client.

</details>

## Install from Source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

Clone or download this repository and open PowerShell in its root directory. Close dnSpy and any clients using this server, then run:

```powershell
.\scripts\build.ps1 -BuildId local-net10 -HostRuntime net10 -DnSpyDir 'D:\Tools\dnSpyEx\bin'
.\scripts\install.ps1 -PackageDir '.\artifacts\build-local-net10' -DnSpyDir 'D:\Tools\dnSpyEx'
```

Build the package first, then install it and use the generated configuration files for Claude Code, Codex or CC Switch. The installer does not invoke build tools or edit client settings.

Pass the **dnSpy installation root** to the installer: the folder containing `dnSpy.exe`.

To build a Framework package without installing it:

```powershell
.\scripts\build.ps1 -BuildId local-net48 -HostRuntime net48 -DnSpyDir 'D:\Tools\dnSpyEx-NetFramework\bin'
```

The build script takes the **`bin` directory**. It prints the output location under `artifacts/build-<id>/`. You can also run `install.cmd` from that directory.

## Updating

Close dnSpy and any clients using this server, extract the new release and run `install.cmd`. The installer updates this plugin's files and preserves other plugins. Existing files are backed up to:

```text
<dnSpy installation>\.dnspy-mcp-backups\install-<id>\
```

Legacy `server/` directories are left intact. Switch the client to the new EXE and verify it before retiring the old directory.

## Troubleshooting

| Problem | Fix |
|---|---|
| The client cannot start the server | Check that the client points to `bin\Extensions\dnSpy.MCP.Server.exe` and that the installation uses one matching release package. |
| The pipe service menu is missing | Match the package to the dnSpy runtime, check `bin\Extensions\`, and restart dnSpy. |
| The AI cannot find a dnSpy session | Check the service status in the MCP menu. Run dnSpy and the MCP client under the same Windows account and privilege level. |
