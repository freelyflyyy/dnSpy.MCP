[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$')]
    [string]$BuildId,

    [string]$DnSpyDir = 'D:\Tools\dnSpyEx\bin'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        throw "[build] dotnet $($Arguments[0]) failed with exit code $exitCode."
    }
}

$repoRoot = (
    Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
).Path

if ([string]::IsNullOrWhiteSpace($BuildId)) {
    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
    $suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $BuildId = "${timestamp}Z-$suffix"
}

Write-Host '[build] [1/5] Checking build inputs...'
$hostContract = Join-Path $DnSpyDir 'dnSpy.Contracts.DnSpy.dll'

if (-not (Test-Path -LiteralPath $hostContract -PathType Leaf)) {
    throw "dnSpy contract DLL was not found: $hostContract"
}

$solution = Join-Path $repoRoot 'dnSpy.MCP.slnx'
$serverProject = Join-Path $repoRoot 'src\dnSpy.MCP.Server\dnSpy.MCP.Server.csproj'

$extensionBuild = Join-Path $repoRoot 'src\dnSpy.MCP.Extension\bin\Release\net10.0-windows'

$artifactsDir = Join-Path $repoRoot 'artifacts'
$packageDir = Join-Path $artifactsDir "build-$BuildId"
$serverOutput = Join-Path $packageDir 'server'
$extensionOutput = Join-Path $packageDir 'extension'

if (Test-Path -LiteralPath $packageDir) {
    throw "Output already exists. Choose another BuildId: $packageDir"
}

New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
New-Item -ItemType Directory -Path $packageDir | Out-Null
New-Item -ItemType Directory `
    -Path $serverOutput, $extensionOutput | Out-Null

# Build all projects in Release configuration.
Write-Host '[build] [2/5] Building all projects in Release mode...'
Invoke-DotNet -Arguments @(
    'build'
    $solution
    '-c'
    'Release'
    '--nologo'
    "-p:DnSpyDir=$DnSpyDir"
    '-v'
    'minimal')

# Publish the standalone server and all its dependencies.
Write-Host '[build] [3/5] Publishing the MCP Server and its dependencies...'
Invoke-DotNet -Arguments @(
    'publish'
    $serverProject
    '-c'
    'Release'
    '--self-contained'
    'false'
    '-p:PublishTrimmed=false'
    '-p:PublishSingleFile=false'
    '-p:PublishAot=false'
    '-o'
    $serverOutput
    '-v'
    'minimal')

# Package only the extension's own required files.
Write-Host '[build] [4/5] Collecting extension files...'
$requiredExtensionFiles = @(
    'dnSpy.MCP.Extension.x.dll'
    'dnSpy.MCP.Extension.x.deps.json'
    'dnSpy.MCP.Protocol.dll'
)

foreach ($name in $requiredExtensionFiles) {
    $source = Join-Path $extensionBuild $name

    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required extension file is missing: $source"
    }

    Copy-Item -LiteralPath $source -Destination $extensionOutput
}

# Include debug symbols when available.
$optionalExtensionFiles = @(
    'dnSpy.MCP.Extension.x.pdb'
    'dnSpy.MCP.Protocol.pdb'
)

foreach ($name in $optionalExtensionFiles) {
    $source = Join-Path $extensionBuild $name

    if (Test-Path -LiteralPath $source -PathType Leaf) {
        Copy-Item -LiteralPath $source -Destination $extensionOutput
    }
}

Write-Host '[build] [5/5] Checking package contents and Protocol consistency...'
$requiredServerFiles = @(
    'dnSpy.MCP.Server.dll'
    'dnSpy.MCP.Server.deps.json'
    'dnSpy.MCP.Server.runtimeconfig.json'
    'dnSpy.MCP.Protocol.dll'
)

foreach ($name in $requiredServerFiles) {
    $path = Join-Path $serverOutput $name

    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required server file is missing: $path"
    }
}

$extensionProtocol = Join-Path $extensionOutput 'dnSpy.MCP.Protocol.dll'
$serverProtocol = Join-Path $serverOutput 'dnSpy.MCP.Protocol.dll'

$extensionHash = (
    Get-FileHash -LiteralPath $extensionProtocol -Algorithm SHA256
).Hash

$serverHash = (
    Get-FileHash -LiteralPath $serverProtocol -Algorithm SHA256
).Hash

if ($extensionHash -ne $serverHash) {
    throw 'The two packaged Protocol assemblies do not match.'
}

Write-Host ''
Write-Host '[build] Package completed successfully.' -ForegroundColor Green
Write-Host "[build] Package directory: $packageDir"
Write-Host "[build] Extension files:   $extensionOutput"
Write-Host "[build] Server files:      $serverOutput"