<#
.SYNOPSIS
Build and install dnSpy.MCP into a selected dnSpyEx installation.

.DESCRIPTION
Use this script to build and deploy dnSpy.MCP into your dnSpyEx installation.
It validates the .NET 10 host and calls build.ps1 with an install-<guid> ID.
For an installation at D:\Tools\dnSpyEx, the destinations are:
  D:\Tools\dnSpyEx\bin\Extensions  - this plugin's files
  D:\Tools\dnSpyEx\server          - complete standalone MCP Server
Existing files are backed up, deployment is verified, and MCP configuration
is printed and saved beneath the server directory for copying into a client.
The script attempts to open the generated configuration in Notepad.

.PARAMETER DnSpyDir
The dnSpyEx installation root or the full path to dnSpy.exe.
For example, D:\Tools\dnSpyEx. Do not pass its bin directory.

.EXAMPLE
.\scripts\install.ps1 'D:\Tools\dnSpyEx'
Build and install using the installation root. Close dnSpy and its MCP clients first.

.EXAMPLE
.\scripts\install.ps1 -DnSpyDir 'D:\Tools\dnSpyEx\dnSpy.exe'
Accepts the executable path as well; the same installation root is used.

.NOTES
REQUIREMENTS
Close dnSpy and stop MCP clients using this Server before installing.
Requires the build environment, .NET 10 Runtime for Server, and write access
to the installation directory. The script does not stop running applications.

BACKUP LOCATION
Each run uses its own directory below the dnSpy installation root:
  <dnSpyRoot>\.dnspy-mcp-backups\install-<guid>\extension\
  <dnSpyRoot>\.dnspy-mcp-backups\install-<guid>\server\
For example: D:\Tools\dnSpyEx\.dnspy-mcp-backups\install-<guid>\.
The final 'Backup directory' message gives the exact directory for that run.
extension contains copies of this plugin's previously existing managed files.
server contains the previous Server directory moved aside during replacement.
On a first installation, extension may be empty and server may not exist.
This is not a backup of the entire dnSpy installation.

RECOVERY
There is no automatic rollback; a failed deployment may leave a partial update.
Close dnSpy and the MCP clients. Preserve the failed server directory under a
different name, then copy the backed-up server directory back as <dnSpyRoot>\server.
Restore only backed-up extension files to <dnSpyRoot>\bin\Extensions, keeping
unrelated plugins. Files newly introduced by the failed install have no old backup.
Confirm both Server and extension come from the same completed installation.

CONFIGURATION
The three-format output section saves files under <dnSpyRoot>\server:
  mcp-claude-code.json - merge mcpServers.dnspy into the project's .mcp.json.
  mcp-codex.toml - merge [mcp_servers.dnspy] into Codex config.toml
    (normally %USERPROFILE%\.codex\config.toml).
  mcp-ccswitch.json - paste into the JSON editor for ONE CC Switch MCP server;
    set its ID/name to dnspy and enable the intended application's sync switch.
The console prints the file paths and configuration contents; Notepad is opened
for each file. Use the format for your chosen client, not all three together.
Merge an existing dnspy entry rather than adding a duplicate or replacing the
whole client configuration. Keep other servers and settings.
This script does not modify Claude Code, Codex, or CC Switch settings directly.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]
    $DnSpyDir
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop';

# Accept either the installation directory or dnSpy.exe.
Write-Host "[install] Checking dnSpy installation: $DnSpyDir"
$inputItem = Get-Item -LiteralPath $DnSpyDir
if ($inputItem.PSIsContainer) {
    $dnSpyRoot = $inputItem.FullName
}else {
    if ($inputItem.Name -ine 'dnSpy.exe') {
        throw 'Please specify the dnSpy directory or dnSpy.exe.';
    }
    $dnSpyRoot = $inputItem.DirectoryName
}

$dnSpyExe = Join-Path $dnSpyRoot 'dnSpy.exe'
$dnSpyBin = Join-Path $dnSpyRoot 'bin'

$requiredFiles = @(
    $dnSpyExe
    (Join-Path $dnSpyBin 'dnSpy.Contracts.DnSpy.dll')
    (Join-Path $dnSpyBin 'dnSpy.Contracts.Logic.dll')
    (Join-Path $dnSpyBin 'dnSpy.runtimeconfig.json')
)

foreach ($path in $requiredFiles) {
    <# $path is the current item #>
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        <# Action to perform if the condition is true #>
        throw "Required dnSpy file was not found: $path"
    }
}

# The current extension targets the .NET 10 host.
$runtimeConfigPath = Join-Path $dnSpyBin 'dnSpy.runtimeconfig.json'
$runtimeConfig = Get-Content -LiteralPath $runtimeConfigPath -Raw | ConvertFrom-Json

if ($runtimeConfig.runtimeOptions.tfm -ne 'net10.0') {
    <# Action to perform if the condition is true #>
    throw 'This installer currently requires the .NET 10 build of dnSpyEx.'
}
Write-Host "[install] Compatible dnSpy installation found: $dnSpyExe"

# Locate the existing build script and repository.
$repoRoot = (
    Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
).Path

$buildScript = Join-Path $PSScriptRoot 'build.ps1'

if (-not (Test-Path -LiteralPath $buildScript -PathType Leaf)) {
    throw "Build script was not found: $buildScript"
}

# Use an explicit ID so we know exactly which package to deploy.
$buildId = 'install-' + [Guid]::NewGuid().ToString('N')

Write-Host '[install] Starting package preparation...'
Write-Host ''
& $buildScript -BuildId $buildId -DnSpyDir $dnSpyBin

$packageRoot = Join-Path $repoRoot "artifacts\build-$buildId"
$extensionSource = Join-Path $packageRoot 'extension'
$serverSource = Join-Path $packageRoot 'server'

foreach ($path in @($extensionSource, $serverSource)) {
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        throw "Package directory was not found: $path"
    }
}

$extensionTarget = Join-Path $dnSpyBin 'Extensions'
$serverTarget = Join-Path $dnSpyRoot 'server'

# Restrict deployment operations to the selected dnSpy directory.
function Assert-DeployPath {
    param([string]$Path)

    $root = [System.IO.Path]::GetFullPath($dnSpyRoot).TrimEnd('\')
    $full = [System.IO.Path]::GetFullPath($Path)

    if ($full -ine $root -and
        -not $full.StartsWith(
            "$root\",
            [StringComparison]::OrdinalIgnoreCase)) {

        throw "Deployment path is outside the dnSpy directory: $full"
    }

    if (Test-Path -LiteralPath $full) {
        $item = Get-Item -LiteralPath $full -Force

        if (($item.Attributes -band
            [System.IO.FileAttributes]::ReparsePoint) -ne 0) {

            throw "Deployment through a link is not supported: $full"
        }
    }
}

$dotnetPath = (
    Get-Command dotnet -CommandType Application -ErrorAction Stop |
        Select-Object -First 1
).Source

$backupBase = Join-Path $dnSpyRoot '.dnspy-mcp-backups'
$backupRoot = Join-Path $backupBase $buildId
$extensionBackup = Join-Path $backupRoot 'extension'
$serverBackup = Join-Path $backupRoot 'server'
$stagedServer = Join-Path $dnSpyRoot ".dnspy-mcp-stage-$buildId"

foreach ($path in @(
    $dnSpyRoot,
    $dnSpyBin,
    $extensionTarget,
    $serverTarget,
    $backupBase,
    $backupRoot,
    $extensionBackup,
    $serverBackup,
    $stagedServer
)) {
    Assert-DeployPath $path
}

if (Test-Path -LiteralPath $stagedServer) {
    throw "Staging directory already exists: $stagedServer"
}

# Do not replace an unrelated directory named "server".
if (Test-Path -LiteralPath $serverTarget) {
    $existingServer = Join-Path $serverTarget 'dnSpy.MCP.Server.dll'

    if (-not (Test-Path -LiteralPath $existingServer -PathType Leaf)) {
        throw "The existing server directory is not recognized: $serverTarget"
    }
}

$requiredPluginFiles = @(
    'dnSpy.MCP.Extension.x.dll'
    'dnSpy.MCP.Extension.x.deps.json'
    'dnSpy.MCP.Protocol.dll'
)

foreach ($name in $requiredPluginFiles) {
    $source = Join-Path $extensionSource $name

    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required plugin file is missing: $source"
    }
}

$pluginFiles = @($requiredPluginFiles)

foreach ($name in @(
    'dnSpy.MCP.Extension.x.pdb',
    'dnSpy.MCP.Protocol.pdb'
)) {
    if (Test-Path -LiteralPath (Join-Path $extensionSource $name) -PathType Leaf) {
        $pluginFiles += $name
    }
}

foreach ($name in $pluginFiles) {
    $destination = Join-Path $extensionTarget $name
    Assert-DeployPath $destination

    if ((Test-Path -LiteralPath $destination) -and
        -not (Test-Path -LiteralPath $destination -PathType Leaf)) {

        throw "Expected a file at: $destination"
    }
}

# Prepare the new Server before changing the installed files.
Write-Host ''
Write-Host '[install] Preparing the new Server files...'

Copy-Item -LiteralPath $serverSource `
    -Destination $stagedServer -Recurse

New-Item -ItemType Directory `
    -Path $extensionTarget, $extensionBackup -Force | Out-Null

# Back up only this plugin's files.
Write-Host '[install] Backing up existing installation files...'

foreach ($name in $pluginFiles) {
    $existingFile = Join-Path $extensionTarget $name

    if (Test-Path -LiteralPath $existingFile -PathType Leaf) {
        Copy-Item -LiteralPath $existingFile `
            -Destination $extensionBackup
    }
}

# Replace the Server directory as a whole to avoid stale dependencies.
Write-Host "[install] Deploying Server to: $serverTarget"

if (Test-Path -LiteralPath $serverTarget) {
    Assert-DeployPath $serverTarget
    Assert-DeployPath $serverBackup

    Move-Item -LiteralPath $serverTarget -Destination $serverBackup
}

Assert-DeployPath $stagedServer
Assert-DeployPath $serverTarget

Move-Item -LiteralPath $stagedServer -Destination $serverTarget

Write-Host "[install] Deploying extension to: $extensionTarget"

foreach ($name in $pluginFiles) {
    Copy-Item -LiteralPath (Join-Path $extensionSource $name) `
        -Destination (Join-Path $extensionTarget $name) -Force
}

# Verify copied plugin files and the main Server assemblies.
Write-Host '[install] Verifying deployed files...'

foreach ($name in $pluginFiles) {
    $sourceHash = (
        Get-FileHash -LiteralPath (Join-Path $extensionSource $name)
    ).Hash

    $targetHash = (
        Get-FileHash -LiteralPath (Join-Path $extensionTarget $name)
    ).Hash

    if ($sourceHash -ne $targetHash) {
        throw "Extension verification failed: $name"
    }
}

foreach ($name in @(
    'dnSpy.MCP.Server.dll',
    'dnSpy.MCP.Protocol.dll'
)) {
    $sourceHash = (
        Get-FileHash -LiteralPath (Join-Path $serverSource $name)
    ).Hash

    $targetHash = (
        Get-FileHash -LiteralPath (Join-Path $serverTarget $name)
    ).Hash

    if ($sourceHash -ne $targetHash) {
        throw "Server verification failed: $name"
    }
}

# Generate separate configuration formats for each client.
$serverDll = Join-Path $serverTarget 'dnSpy.MCP.Server.dll'

$serverConfig = [ordered]@{
    type = 'stdio'
    command = $dotnetPath
    args = @($serverDll)
}

# Claude Code: project-level .mcp.json structure.
$claudeConfig = [ordered]@{
    mcpServers = [ordered]@{
        dnspy = $serverConfig
    }
}

$claudeJson = $claudeConfig | ConvertTo-Json -Depth 6

# Codex: escape Windows paths as TOML basic strings.
function ConvertTo-TomlString {
    param([string]$Value)

    return '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

$codexToml = @"
[mcp_servers.dnspy]
command = $(ConvertTo-TomlString $dotnetPath)
args = [$(ConvertTo-TomlString $serverDll)]
cwd = $(ConvertTo-TomlString $serverTarget)
"@

# CC Switch: one server object, without the mcpServers wrapper.
$ccswitchConfig = [ordered]@{
    type = 'stdio'
    command = $dotnetPath
    args = @($serverDll)
    cwd = $serverTarget
}

$ccswitchJson = $ccswitchConfig | ConvertTo-Json -Depth 5

$configurations = @(
    [pscustomobject]@{
        Name = 'Claude Code'
        Path = Join-Path $serverTarget 'mcp-claude-code.json'
        Content = $claudeJson
        Usage = 'Merge mcpServers.dnspy into your project .mcp.json.'
    }

    [pscustomobject]@{
        Name = 'Codex'
        Path = Join-Path $serverTarget 'mcp-codex.toml'
        Content = $codexToml
        Usage = 'Merge [mcp_servers.dnspy] into your Codex config.toml.'
    }

    [pscustomobject]@{
        Name = 'CC Switch'
        Path = Join-Path $serverTarget 'mcp-ccswitch.json'
        Content = $ccswitchJson
        Usage = 'Paste into ONE MCP server JSON editor; name it dnspy and enable app sync.'
    }
)

$utf8 = [System.Text.UTF8Encoding]::new($false)

Write-Host '[install] Saving client configuration files...'

foreach ($configuration in $configurations) {
    Assert-DeployPath $configuration.Path

    [System.IO.File]::WriteAllText(
        $configuration.Path,
        $configuration.Content,
        $utf8
    )
}

Write-Host ''
Write-Host '[install] Installation completed successfully.' -ForegroundColor Green
Write-Host "[install] Backup directory: $backupRoot"
Write-Host '[install] Start dnSpy and enable its pipe service.'

foreach ($configuration in $configurations) {
    Write-Host ''
    Write-Host "[install] Configuration for $($configuration.Name)" -ForegroundColor Cyan
    Write-Host "[install] File: $($configuration.Path)"
    Write-Host "[install] $($configuration.Usage)"
    Write-Host '[install] Preserve other settings and avoid duplicate dnspy entries.'
    Write-Host ''
    Write-Host $configuration.Content

    try {
        Start-Process -FilePath 'notepad.exe' `
            -ArgumentList ('"{0}"' -f $configuration.Path) `
            -WindowStyle Normal | Out-Null
    }
    catch {
        Write-Warning "Could not open configuration. Open manually: $($configuration.Path)"
    }
}