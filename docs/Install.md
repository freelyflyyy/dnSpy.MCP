<div align="center">

# Installation & Setup

[**English**](Install.md) | [简体中文](Install_CN.md)
</div>

---

This guide covers building `dnSpy.MCP` from source, deploying the extension and server, and connecting an MCP client. The examples below use `D:\Tools\dnSpyEx` as the dnSpy installation path—adjust it to match your setup.

## Prerequisites

| Component | Requirement |
| :--- | :--- |
| Operating System | Windows |
| dnSpy Host | dnSpyEx `.NET 10` build (tested on v6.6.0; `.NET Framework` builds are not supported) |
| Build SDK | .NET 10 SDK (with Windows desktop workload) |
| Server Runtime | System-installed .NET 10 Runtime |
| MCP Client | Any client supporting local `stdio` MCP servers |

> **Note:** The extension targets `net10.0-windows`. Also note that dnSpyEx's bundled runtime is private to dnSpy; the standalone MCP server requires a system-wide .NET 10 runtime to run.

---

## 1. Build from Source

Clone the repository, open PowerShell in the repo root, and run the build script:

```powershell
.\scripts\build.ps1 -BuildId manual-001 -DnSpyDir 'D:\Tools\dnSpyEx\bin'
```

* `-DnSpyDir` must point to the `bin` folder containing `dnSpy.Contracts.DnSpy.dll`, not the root folder where `dnSpy.exe` sits.
* `-BuildId` is optional. Existing output folders are never overwritten; omit `-BuildId` to generate a timestamped folder automatically, or pass a new name for subsequent builds.

The script builds the solution in `Release` mode and stages the output under `artifacts/`:

```text
artifacts/build-manual-001/
├─ extension/
└─ server/
```

---

## 2. Deploy Extension & Server

Before copying files, close dnSpy and stop any MCP client currently running the old server binary.

1. Copy everything inside `artifacts/build-manual-001/extension/` to `D:\Tools\dnSpyEx\bin\Extensions\` (create the `Extensions` folder if it doesn't exist).
2. Copy the entire `artifacts/build-manual-001/server/` folder to `D:\Tools\dnSpyEx\server\`.

Your directory layout should look like this:

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
   └─ (other published dependencies and runtimes)
```

> **Updating:** Always update the extension, `dnSpy.MCP.Protocol.dll`, and the `server/` directory together from the same build. Keep all files in `server/` intact—copying `dnSpy.MCP.Server.dll` alone will not work.

Launch `dnSpy.exe`, load any .NET assembly, and click **View → 打开/关闭管道服务** (Toggle Pipe Service) from the top menu. Check the **Output** pane for the active pipe name:

```text
dnSpy-Mcp-12345
```

The number at the end is the current dnSpy process ID (PID), which changes every time dnSpy restarts.

---

## 3. Configure MCP Client

Point your MCP client at the server using `dotnet.exe`:

| Field | Value |
| :--- | :--- |
| Server Name | `dnspy` |
| Transport | `stdio` |
| Command | `C:\Program Files\dotnet\dotnet.exe` |
| Arguments | `D:\Tools\dnSpyEx\server\dnSpy.MCP.Server.dll` |

Example JSON configuration:

```json
{
  "type": "stdio",
  "command": "C:\\Program Files\\dotnet\\dotnet.exe",
  "args": [
    "D:\\Tools\\dnSpyEx\\server\\dnSpy.MCP.Server.dll"
  ]
}
```

If you manage configs with CC Switch, paste the JSON object above into the single-server editor and enable sync for your target client.

The server starts with no extra CLI flags; target PIDs and scripts are passed dynamically via MCP tool calls. Make sure both the MCP client and dnSpy run under the same Windows user account and privilege level (e.g., both normal or both elevated).

---

## 4. Verify the Setup

Once connected, your client should see three tools:

| Tool | Purpose |
| :--- | :--- |
| `search_api` | Query built-in API docs (e.g., search `ListAssemblies` using English keywords). |
| `session` | List candidate dnSpy PIDs that currently have a pipe open. |
| `execute_csharp` | Run a C# script inside the specified dnSpy process. |

Test the connection by calling `execute_csharp` (replace `12345` with the PID from `session`):

```json
{
  "targetProcessId": 12345,
  "code": "return 1 + 2;"
}
```

Expected response:

```json
{"Success":true,"Value":3,"Error":null}
```

Next, run `return ctx.Documents.ListAssemblies();` to confirm access to dnSpy's loaded documents (returns `[]` if no assembly is open). If you have multiple dnSpy windows open, verify the target PID before running scripts.

---

## Troubleshooting

| Issue | What to Check |
| :--- | :--- |
| Server fails to start | Check the `dotnet.exe` path, verify .NET 10 Runtime is installed system-wide, and ensure the `server/` folder isn't missing files. |
| Menu item missing in dnSpy | Confirm you are running the `.NET 10` build of dnSpyEx, verify files are in `bin\Extensions\`, and restart dnSpy. |
| `session` returns `[]` | Make sure dnSpy is running and **View → 打开/关闭管道服务** has been clicked. |
| PID shows up, but script fails | The process may have exited, or dnSpy and the MCP client are running at different privilege levels (Admin vs. non-Admin). |
| Script request times out | A timeout only stops the client from waiting—the script itself may still be running on dnSpy's UI thread. |