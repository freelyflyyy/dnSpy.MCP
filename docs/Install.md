<div align="center">

# Installation & Setup

[**English**](Install.md) | [简体中文](Install_CN.md)

</div>

## Quick Start

These examples use Windows x64 with dnSpy installed at `D:\Tools\dnSpyEx`. Adjust the paths for your setup.

### 1. Prepare the environment

- Download `dnSpy-net-win64.zip` from [dnSpyEx 6.6.0](https://github.com/dnSpyEx/dnSpy/releases/tag/v6.6.0) and extract it. The extension currently supports the .NET 10 build; .NET Framework builds are not supported.
- Install the Windows x64 [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Skip this if you already have the .NET 10 SDK or Desktop Runtime.

### 2. Install the extension

Download `dnSpy-MCP-v<version>-windows.zip` from [Releases](https://github.com/freelyflyyy/dnSpy.MCP/releases) and extract it.

Close dnSpy and any MCP clients using this server, then copy:

| Package contents | Destination |
|---|---|
| All files inside `extension/` | `D:\Tools\dnSpyEx\bin\Extensions\` |
| The entire `server/` folder | `D:\Tools\dnSpyEx\server\` |

Create `Extensions` if it does not exist. Keep all dependencies and subfolders inside `server/`.

### 3. Start using it

1. Launch dnSpy and open the .NET assembly you want to analyze.
2. Click **View → 打开/关闭管道服务** (Toggle Pipe Service).
3. Configure your MCP client below, save, and restart the client.

Once connected, ask your AI:

> Use dnspy to list the currently loaded assemblies.

Enable the pipe service again whenever you restart dnSpy.

## Client Configuration

Choose one setup method. Replace the sample dnSpy path, and update `command` if `dotnet.exe` is installed elsewhere.

If you already have a configuration, add or update the `dnspy` entry while keeping your other servers.

<details>
<summary>Codex</summary>

Add this to `%USERPROFILE%\.codex\config.toml`:

```toml
[mcp_servers.dnspy]
command = "C:\\Program Files\\dotnet\\dotnet.exe"
args = ["D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"]
cwd = "D:\\Tools\\dnSpyEx\\server"
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
      "command": "C:\\Program Files\\dotnet\\dotnet.exe",
      "args": ["D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"]
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
  "command": "C:\\Program Files\\dotnet\\dotnet.exe",
  "args": ["D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"],
  "cwd": "D:\\Tools\\dnSpyEx\\server"
}
```

Save, enable synchronization for your target client, and restart that client.

</details>

## Install from Source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

Clone or download this repository and open PowerShell in its root directory. Close dnSpy and any clients using this server, then run:

```powershell
.\scripts\install.ps1 -DnSpyDir 'D:\Tools\dnSpyEx'
```

The script builds and deploys the project, then generates and opens configuration files for Claude Code, Codex, and CC Switch. Add the appropriate configuration to your client and enable the pipe service as described above.

Pass the **dnSpy installation root**: the folder containing `dnSpy.exe`.

To build a package without installing it:

```powershell
.\scripts\build.ps1 -DnSpyDir 'D:\Tools\dnSpyEx\bin'
```

The build script takes the **`bin` directory**. It prints the output location under `artifacts/build-<id>/`. Copy its contents to the destinations listed in Quick Start.

## Updating

Close dnSpy and any clients using this server. Back up the old files, then update the extension files and replace the entire `server/` directory using the same release. Keep other extensions intact.

When installing from source, the installer automatically backs up existing files to:

```text
<dnSpy installation>\.dnspy-mcp-backups\install-<id>\
```

## Troubleshooting

| Problem | Fix |
|---|---|
| The client cannot start the server | Check the configured paths, .NET 10 Runtime, and that the entire `server/` folder was copied. |
| The pipe service menu is missing | Use the .NET 10 build of dnSpyEx, check `bin\Extensions\`, and restart dnSpy. |
| The AI cannot find a dnSpy session | Enable the pipe service. Run dnSpy and the MCP client under the same Windows account and privilege level. |