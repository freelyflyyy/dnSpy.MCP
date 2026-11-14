#Requires -Version 5.1
<#
.SYNOPSIS
Build a flat, runtime-specific release package. Does not install it.
.EXAMPLE
.\scripts\build.ps1 -HostRuntime net48 -DnSpyDir 'D:\Tools\dnSpyEx-NetFramework\bin' -BuildId local-48
#>
[CmdletBinding()]
param (
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$')]
    [string]
    $BuildId,

    [ValidateSet('net10', 'net48')]
    [string]
    $HostRuntime = 'net10',

    [string]
    $DnSpyDir
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    param (
        [string[]]
        $Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments[0]) failed ($LASTEXITCODE)."
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $BuildId) {
    $BuildId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
}

if (-not $DnSpyDir) {
    $DnSpyDir = if ($HostRuntime -eq 'net48') {
        'D:\Tools\dnSpyEx-NetFramework\bin'
    } else {
        'D:\Tools\dnSpyEx\bin'
    }
}

$DnSpyDir = (Resolve-Path -LiteralPath $DnSpyDir).Path
$hostRoot = Split-Path -Parent $DnSpyDir
$contract = Join-Path $DnSpyDir 'dnSpy.Contracts.DnSpy.dll'
$runtimeConfig = Join-Path $DnSpyDir 'dnSpy.runtimeconfig.json'
if (-not (Test-Path -LiteralPath $contract -PathType Leaf)) {
    throw "Missing dnSpy contracts: $contract"
}

if ($HostRuntime -eq 'net10') {
    if (-not (Test-Path -LiteralPath $runtimeConfig)) {
        throw 'Expected the .NET 10 dnSpy bin directory.'
    }
    if ((Get-Content -LiteralPath $runtimeConfig -Raw | ConvertFrom-Json).runtimeOptions.tfm -ne 'net10.0') {
        throw 'Expected a net10.0 host.'
    }
} else {
    $config = Join-Path $hostRoot 'dnSpy.exe.config'
    if ((Test-Path -LiteralPath $runtimeConfig) -or -not (Test-Path -LiteralPath $config)) {
        throw 'Expected the .NET Framework dnSpy bin directory.'
    }
    [xml]$xml = Get-Content -LiteralPath $config -Raw
    if ($xml.configuration.startup.supportedRuntime.sku -ne '.NETFramework,Version=v4.8') {
        throw 'Expected a .NET Framework 4.8 host.'
    }
}

$tfm = if ($HostRuntime -eq 'net48') {
    'net48'
} else {
    'net10.0-windows'
}

$referenceProperty = if ($HostRuntime -eq 'net48') {
    'DnSpyNet48Dir'
} else {
    'DnSpyNet10Dir'
}

$packageDir = Join-Path $repoRoot "artifacts\build-$BuildId"
$stage = Join-Path $repoRoot "artifacts\stage-$BuildId"
foreach ($path in @($packageDir, $stage)) {
    if (Test-Path -LiteralPath $path) {
        throw "Output exists; choose another BuildId: $path"
    }
}
[void][IO.Directory]::CreateDirectory($stage)
[void][IO.Directory]::CreateDirectory($packageDir)
$extensionStage = Join-Path $stage 'extension'
$serverStage = Join-Path $stage 'server'
$extensionProject = Join-Path $repoRoot 'src\dnSpy.MCP.Extension\dnSpy.MCP.Extension.csproj'
$serverProject = Join-Path $repoRoot 'src\dnSpy.MCP.Server\dnSpy.MCP.Server.csproj'

Write-Host "[build] [1/4] Publishing $tfm extension..."
Invoke-DotNet @('publish', $extensionProject, '-c', 'Release', '-f', $tfm,
    "-p:${referenceProperty}=$DnSpyDir", '-p:CopyLocalLockFileAssemblies=true',
    '-o', $extensionStage, '--nologo', '-v', 'minimal')
Write-Host '[build] [2/4] Publishing self-contained, single-file Windows x64 Server...'
Invoke-DotNet @('publish', $serverProject, '-c', 'Release', '-r', 'win-x64',
    '--self-contained', 'true', '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:PublishTrimmed=false',
    '-p:PublishAot=false', '-o', $serverStage, '--nologo', '-v', 'minimal')
$serverExe = Join-Path $serverStage 'dnSpy.MCP.Server.exe'
if (-not (Test-Path -LiteralPath $serverExe -PathType Leaf)) {
    throw 'Server EXE is missing.'
}

$looseRuntimeFiles = @(Get-ChildItem -LiteralPath $serverStage -File | Where-Object {
    $_.Name -ne 'dnSpy.MCP.Server.exe' -and $_.Extension -ne '.pdb'
})
if ($looseRuntimeFiles.Count) {
    throw "Server is not a single-file deployment: $($looseRuntimeFiles.Name -join ', ')"
}

Write-Host '[build] [3/4] Collecting plugin files and recording host-provided dependencies...'
$hostDependencies = @{}
foreach ($name in @('dnSpy.Contracts.DnSpy.dll', 'dnSpy.Contracts.Logic.dll')) {
    $hostDependencies[$name] = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $DnSpyDir $name)).FullName
}

foreach ($file in (Get-ChildItem -LiteralPath $extensionStage -File | Where-Object {
    $_.Extension -eq '.dll' -or $_.Name -eq 'dnSpy.MCP.Extension.x.deps.json'
})) {
    $hostFile = Join-Path $DnSpyDir $file.Name
    # Reuse a host assembly only when its full strong-name identity matches.
    # Different versions are kept plugin-local; never overwrite the host's bin files.
    if ($file.Extension -eq '.dll' -and -not $file.Name.StartsWith('dnSpy.MCP.', [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $hostFile -PathType Leaf)) {
        $builtIdentity = [Reflection.AssemblyName]::GetAssemblyName($file.FullName).FullName
        $hostIdentity = [Reflection.AssemblyName]::GetAssemblyName($hostFile).FullName
        if ($builtIdentity -ceq $hostIdentity) {
            $hostDependencies[$file.Name] = $hostIdentity
            continue
        }
    }
    Copy-Item -LiteralPath $file.FullName -Destination $packageDir
}
Copy-Item -LiteralPath $serverExe -Destination $packageDir
$required = @('dnSpy.MCP.Server.exe', 'dnSpy.MCP.Extension.x.dll', 'dnSpy.MCP.Protocol.dll', 'dnSpy.MCP.Logging.dll')
if ($HostRuntime -eq 'net10') {
    $required += 'dnSpy.MCP.Extension.x.deps.json'
}

foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $packageDir $name) -PathType Leaf)) {
        throw "Missing package file: $name"
    }
}

$files = @(Get-ChildItem -LiteralPath $packageDir -File | Sort-Object Name | ForEach-Object {
    [ordered]@{
        name = $_.Name
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
})
[xml]$props = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props') -Raw
$manifest = [ordered]@{
    schemaVersion = 1
    product = 'dnSpy.MCP'
    version = [string]$props.Project.PropertyGroup.Version
    hostRuntime = $HostRuntime
    architecture = 'win-x64'
    server = 'dnSpy.MCP.Server.exe'
    selfContained = $true
    files = $files
    hostDependencies = @($hostDependencies.Keys | Sort-Object | ForEach-Object {
        [ordered]@{
            name = $_
            assemblyIdentity = $hostDependencies[$_]
        }
    })
}

$utf8 = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $packageDir 'package.json'), ($manifest | ConvertTo-Json -Depth 6), $utf8)
foreach ($name in @('install.ps1', 'install.cmd')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $packageDir
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE.txt') -Destination $packageDir
Copy-Item -LiteralPath (Join-Path $repoRoot 'CHANGELOG.md') -Destination $packageDir
$readme = @"
dnSpy.MCP $($manifest.version) - $HostRuntime - Windows x64
Close dnSpy and the MCP Server, then run install.cmd and choose dnSpy.exe or its folder.
This package does not require Visual Studio, a .NET SDK or a separately installed .NET 10 runtime.
The net48 host still requires .NET Framework 4.8.
The Server contains its own runtime; native runtime components may extract into the user's temporary directory.
The installer writes only plugin-owned runtime files under bin\Extensions and preserves other plugins.
Existing installations are backed up. Generated MCP configurations use dnSpy.MCP.Server.exe with no arguments.
The installer prints configuration files; it does not edit your MCP client's settings.
For installation details, see docs/Install.md in the repository; do not overwrite host bin DLLs.
"@
[IO.File]::WriteAllText((Join-Path $packageDir 'README.txt'), $readme, $utf8)
Write-Host "[build] [4/4] Package complete: $packageDir"
