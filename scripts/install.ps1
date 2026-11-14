#Requires -Version 5.1
<#
.SYNOPSIS
Install a prebuilt flat package. Never builds source or requires dotnet.
.EXAMPLE
.\scripts\install.ps1 -PackageDir .\artifacts\build-local -DnSpyDir D:\Tools\dnSpyEx
#>
[CmdletBinding()]
param (
    [Parameter(Position = 0)]
    [string]
    $DnSpyDir,

    [string]
    $PackageDir = $PSScriptRoot,

    [switch]
    $ValidateOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-PlainPath {
    param (
        [string]
        $Path
    )

    $current = [IO.Path]::GetFullPath($Path)
    while ($current) {
        if (Test-Path -LiteralPath $current) {
            if (((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Links/reparse points are not supported: $current"
            }
        }
        $parent = [IO.Path]::GetDirectoryName($current)
        if ($parent -eq $current) {
            break
        }
        $current = $parent
    }
}

function Assert-FileName {
    param (
        [string]
        $Name
    )

    if ($Name -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$' -or $Name -match '\.\.') {
        throw "Invalid package filename: $Name"
    }
}

function Read-Json {
    param (
        [string]
        $Path
    )

    Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Hash {
    param (
        [string]
        $Path
    )

    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function TomlString {
    param (
        [string]
        $Value
    )

    '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

if (-not [Environment]::Is64BitOperatingSystem) {
    throw 'This package requires Windows x64.'
}

if (-not $DnSpyDir) {
    if ($ValidateOnly) {
        throw 'Specify -DnSpyDir for validation.'
    }
    $DnSpyDir = (Read-Host 'dnSpy folder or full path to dnSpy.exe').Trim('"')
}

if ([string]::IsNullOrWhiteSpace($PackageDir)) {
    $PackageDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$PackageDir = (Resolve-Path -LiteralPath $PackageDir).Path
Assert-PlainPath $PackageDir
$manifestPath = Join-Path $PackageDir 'package.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'package.json is missing. Build with scripts/build.ps1 first, or use an extracted release package.'
}

$manifest = Read-Json $manifestPath
if ($manifest.schemaVersion -ne 1 -or $manifest.product -cne 'dnSpy.MCP' -or
    $manifest.hostRuntime -notin @('net10', 'net48') -or $manifest.architecture -ne 'win-x64' -or
    $manifest.server -cne 'dnSpy.MCP.Server.exe' -or $manifest.selfContained -ne $true) {
    throw 'Unsupported package manifest.'
}

$item = Get-Item -LiteralPath $DnSpyDir
if ($item.PSIsContainer) {
    $root = $item.FullName
} elseif ($item.Name -ieq 'dnSpy.exe') {
    $root = $item.DirectoryName
} else {
    throw 'Select dnSpy.exe or its installation directory.'
}
Assert-PlainPath $root
$bin = Join-Path $root 'bin'
$target = Join-Path $bin 'Extensions'
Assert-PlainPath $target
if ($PackageDir -ieq $target) {
    throw 'Extract the package outside the installation directory before installing.'
}

foreach ($p in @((Join-Path $root 'dnSpy.exe'), (Join-Path $bin 'dnSpy.Contracts.DnSpy.dll'))) {
    if (-not (Test-Path -LiteralPath $p -PathType Leaf)) {
        throw "Missing host file: $p"
    }
}

$runtimeConfig = Join-Path $bin 'dnSpy.runtimeconfig.json'
if (Test-Path -LiteralPath $runtimeConfig) {
    if ((Read-Json $runtimeConfig).runtimeOptions.tfm -ne 'net10.0') {
        throw 'Unsupported dnSpy runtime.'
    }
    $hostRuntime = 'net10'
} else {
    [xml]$config = Get-Content -LiteralPath (Join-Path $root 'dnSpy.exe.config') -Raw
    if ($config.configuration.startup.supportedRuntime.sku -ne '.NETFramework,Version=v4.8') {
        throw 'Expected .NET Framework 4.8.'
    }
    $hostRuntime = 'net48'
}

if ($hostRuntime -ne $manifest.hostRuntime) {
    throw "Wrong package: package=$($manifest.hostRuntime), host=$hostRuntime."
}

$payload = @{}
foreach ($entry in @($manifest.files)) {
    Assert-FileName $entry.name
    if ($entry.name -notmatch '\.(dll|exe|json)$' -or $entry.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or $payload.ContainsKey($entry.name)) {
        throw "Invalid or duplicate runtime entry: $($entry.name)"
    }
    $source = Join-Path $PackageDir $entry.name
    Assert-PlainPath $source
    if (-not (Test-Path -LiteralPath $source -PathType Leaf) -or (Hash $source) -ine $entry.sha256) {
        throw "Package checksum mismatch: $($entry.name)"
    }
    $payload[$entry.name] = $entry
}

foreach ($name in @('dnSpy.MCP.Server.exe', 'dnSpy.MCP.Extension.x.dll', 'dnSpy.MCP.Protocol.dll', 'dnSpy.MCP.Logging.dll')) {
    if (-not $payload.ContainsKey($name)) {
        throw "Missing runtime entry: $name"
    }
}

if ($hostRuntime -eq 'net10' -and -not $payload.ContainsKey('dnSpy.MCP.Extension.x.deps.json')) {
    throw 'Missing extension deps file.'
}

foreach ($dependency in @($manifest.hostDependencies)) {
    Assert-FileName $dependency.name
    $path = Join-Path $bin $dependency.name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        [Reflection.AssemblyName]::GetAssemblyName($path).FullName -cne $dependency.assemblyIdentity) {
        throw "Host dependency mismatch: $($dependency.name). Use the supported dnSpy build."
    }
}

# Print/save configurations for the executable; never change clients' settings.
$exe = Join-Path $target 'dnSpy.MCP.Server.exe'
# Serialize scalar values for escaping; use templates for consistent PS5.1/PS7 formatting.
$commandJson = ConvertTo-Json -InputObject $exe -Compress
$cwdJson = ConvertTo-Json -InputObject $target -Compress

$configs = @{
    'mcp-claude-code.json' = @"
{
    "mcpServers": {
        "dnspy": {
            "command": $commandJson,
            "args": []
        }
    }
}
"@
    'mcp-ccswitch.json' = @"
{
    "type": "stdio",
    "command": $commandJson,
    "args": [],
    "cwd": $cwdJson
}
"@
    'mcp-codex.toml' = "[mcp_servers.dnspy]`r`ncommand = $(TomlString $exe)`r`nargs = []`r`ncwd = $(TomlString $target)`r`n"
}

foreach ($name in $configs.Keys) {
    if ($payload.ContainsKey($name)) {
        throw "Reserved configuration name: $name"
    }
}

$statePath = Join-Path $target '.dnspy-mcp-install.json'
$previous = @{}
if (Test-Path -LiteralPath $statePath) {
    Assert-PlainPath $statePath
    $old = Read-Json $statePath
    if ($old.product -ne 'dnSpy.MCP' -or $old.schemaVersion -ne 1) {
        throw 'Unrecognized installation state.'
    }
    foreach ($entry in @($old.files)) {
        Assert-FileName $entry.name
        if ($entry.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or $entry.owned -isnot [bool] -or $previous.ContainsKey($entry.name)) {
            throw 'Invalid installation state.'
        }
        $previous[$entry.name] = $entry
    }
}

$names = @(@($payload.Keys) + @($configs.Keys) + @($previous.Keys) | Sort-Object -Unique)
foreach ($name in $names) {
    $path = Join-Path $target $name
    Assert-PlainPath $path
    if (-not (Test-Path -LiteralPath $path)) {
        continue
    }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Expected a file: $path"
    }
    if ($previous.ContainsKey($name) -and $previous[$name].owned) {
        if ((Hash $path) -ine $previous[$name].sha256) {
            throw "Installed file was modified; back it up before upgrading: $path"
        }
    } elseif ($payload.ContainsKey($name) -and (Hash $path) -ieq $payload[$name].sha256) {
        # Equal shared files are not adopted as plugin-owned.
    } elseif (-not $previous.ContainsKey($name) -and $name -match '^dnSpy\.MCP\.(Extension\.x|Protocol|Logging)\.(dll|deps\.json)$') {
        # Recognized legacy plugin files are backed up before migration.
    } else {
        throw "Refusing to replace an unowned file: $path"
    }
}

Write-Host "[install] Validated $($manifest.version) / $hostRuntime -> $target"
if ($ValidateOnly) {
    return
}

# Do not stop applications or overwrite files in an active installation.
foreach ($process in @(Get-Process -Name 'dnSpy', 'dnSpy-x86', 'dnSpy.MCP.Server' -ErrorAction SilentlyContinue)) {
    try {
        $processPath = $process.Path
    } catch {
        throw "Cannot inspect process $($process.Id); close dnSpy and MCP Server first."
    }
    if (-not $processPath -or $processPath.StartsWith($root.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Close dnSpy and its MCP Server before installation (PID $($process.Id))."
    }
}

$backup = Join-Path $root ('.dnspy-mcp-backups\install-' + [Guid]::NewGuid().ToString('N'))
Assert-PlainPath $backup
[void][IO.Directory]::CreateDirectory($backup)
[void][IO.Directory]::CreateDirectory($target)
$affected = @($names | Where-Object {
    $payload.ContainsKey($_) -or $configs.ContainsKey($_) -or ($previous.ContainsKey($_) -and $previous[$_].owned)
})
# Copy backups before the first write; rollback restores only files changed by this run.
foreach ($name in @($affected) + @('.dnspy-mcp-install.json')) {
    $path = Join-Path $target $name
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Copy-Item -LiteralPath $path -Destination (Join-Path $backup $name)
    }
}

$changed = New-Object 'System.Collections.Generic.List[string]'
$utf8 = New-Object System.Text.UTF8Encoding($false)
try {
    $installed = @(
    )

    foreach ($name in $affected) {
        $path = Join-Path $target $name
        if (-not $payload.ContainsKey($name) -and -not $configs.ContainsKey($name)) {
            if (Test-Path -LiteralPath $path) {
                $changed.Add($name)
                Remove-Item -LiteralPath $path
            }
            continue
        }
        $owned = -not (Test-Path -LiteralPath $path) -or
            ($previous.ContainsKey($name) -and $previous[$name].owned) -or
            (-not $previous.ContainsKey($name) -and $name -match '^dnSpy\.MCP\.')
        if ($configs.ContainsKey($name)) {
            $changed.Add($name)
            [IO.File]::WriteAllText($path, $configs[$name], $utf8)
            $owned = $true
        } elseif (-not (Test-Path -LiteralPath $path) -or (Hash $path) -ine $payload[$name].sha256) {
            $changed.Add($name)
            Copy-Item -LiteralPath (Join-Path $PackageDir $name) -Destination $path -Force
        }
        $hash = Hash $path
        if ($payload.ContainsKey($name) -and $hash -ine $payload[$name].sha256) {
            throw "Installed checksum mismatch: $name"
        }
        $installed += [ordered]@{
            name = $name
            sha256 = $hash
            owned = [bool]$owned
        }
    }
    $state = [ordered]@{
        schemaVersion = 1
        product = 'dnSpy.MCP'
        version = $manifest.version
        hostRuntime = $hostRuntime
        files = $installed
    }
    $changed.Add('.dnspy-mcp-install.json')
    [IO.File]::WriteAllText($statePath, ($state | ConvertTo-Json -Depth 6), $utf8)
} catch {
    $installError = $_
    $rollbackErrors = @(
    )

    foreach ($name in $changed) {
        try {
            $path = Join-Path $target $name
            $saved = Join-Path $backup $name
            if (Test-Path -LiteralPath $saved) {
                Copy-Item -LiteralPath $saved -Destination $path -Force
            } elseif (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path
            }
        } catch {
            $rollbackErrors += $_.Exception.Message
        }
    }
    if ($rollbackErrors.Count) {
        Write-Warning ("Rollback incomplete: " + ($rollbackErrors -join '; '))
    }
    throw "Installation failed: $($installError.Exception.Message). Backup: $backup"
}

Write-Host "[install] Complete. Backup: $backup"
Write-Host '[install] Start dnSpy; its MCP service starts automatically.'
Write-Host '[install] Update your MCP client to use the EXE. Preserve unrelated client settings.'
foreach ($name in ($configs.Keys | Sort-Object)) {
    Write-Host (Join-Path $target $name)
    Write-Host $configs[$name]
}

if (Test-Path -LiteralPath (Join-Path $root 'server\dnSpy.MCP.Server.dll')) {
    Write-Host '[install] A legacy server directory was left intact. Retire its client entry after verifying the new EXE.'
}
