#Requires -Version 7.2
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $PackageDir,

    [Parameter(Mandatory)]
    [string]
    $DnSpyDir
)
$ErrorActionPreference = 'Stop'

$PackageDir = (Resolve-Path -LiteralPath $PackageDir).Path
$DnSpyDir = (Resolve-Path -LiteralPath $DnSpyDir).Path
$manifest = Get-Content (Join-Path $PackageDir 'package.json') -Raw | ConvertFrom-Json
if (@(Get-ChildItem -LiteralPath $PackageDir -Directory).Count) {
    throw 'Release package must be flat.'
}

if (Test-Path (Join-Path $PackageDir 'dnSpy.MCP.Server.dll')) {
    throw 'Loose Server DLL is not allowed.'
}

foreach ($file in $manifest.files) {
    if ((Get-FileHash -LiteralPath (Join-Path $PackageDir $file.name)).Hash -ine $file.sha256) {
        throw "Bad payload: $($file.name)"
    }
}

$repo = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $repo ('artifacts\package-tests-' + [Guid]::NewGuid().ToString('N'))
$fixture = Join-Path $testRoot 'dnSpy fixture'
$fixtureBin = Join-Path $fixture 'bin'
[void][IO.Directory]::CreateDirectory($fixtureBin)
$realRoot = Split-Path -Parent $DnSpyDir
Copy-Item -LiteralPath (Join-Path $realRoot 'dnSpy.exe') -Destination $fixture
foreach ($name in @('dnSpy.exe.config')) {
    $path = Join-Path $realRoot $name
    if (Test-Path -LiteralPath $path) {
        Copy-Item -LiteralPath $path -Destination $fixture
    }
}

$runtimeConfig = Join-Path $DnSpyDir 'dnSpy.runtimeconfig.json'
if (Test-Path -LiteralPath $runtimeConfig) {
    Copy-Item -LiteralPath $runtimeConfig -Destination $fixtureBin
}

foreach ($name in @(@($manifest.hostDependencies | ForEach-Object name) + @('dnSpy.Contracts.DnSpy.dll') | Sort-Object -Unique)) {
    Copy-Item -LiteralPath (Join-Path $DnSpyDir $name) -Destination $fixtureBin
}

$target = Join-Path $fixtureBin 'Extensions'
[void][IO.Directory]::CreateDirectory($target)
$sentinel = Join-Path $target 'unrelated-plugin.txt'
[IO.File]::WriteAllText($sentinel, 'preserve me')
$winPS = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$installer = Join-Path $PackageDir 'install.ps1'
function Invoke-Install {
    param (
        [string]
        $Package,

        [bool]
        $ShouldSucceed = $true
    )

    & $winPS -NoProfile -ExecutionPolicy Bypass -File $installer -PackageDir $Package -DnSpyDir $fixture
    $ok = $LASTEXITCODE -eq 0
    if ($ok -ne $ShouldSucceed) {
        throw "Unexpected installer exit: $LASTEXITCODE"
    }
    $global:LASTEXITCODE = 0
}

$oldPath = $env:PATH
$oldRoot = $env:DOTNET_ROOT
$oldRoot64 = $env:DOTNET_ROOT_X64
try {
    # Release installation uses Windows PowerShell only; hide dotnet from lookup.
    $env:PATH = "$env:SystemRoot\System32"
    $env:DOTNET_ROOT = Join-Path $testRoot 'no-installed-runtime'
    $env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
    Invoke-Install $PackageDir
    Invoke-Install $PackageDir
    foreach ($file in $manifest.files) {
        if ((Get-FileHash -LiteralPath (Join-Path $target $file.name)).Hash -ine $file.sha256) {
            throw 'Installed payload differs.'
        }
    }
    $config = Get-Content (Join-Path $target 'mcp-claude-code.json') -Raw | ConvertFrom-Json
    if ($config.mcpServers.dnspy.command -ne (Join-Path $target 'dnSpy.MCP.Server.exe') -or @($config.mcpServers.dnspy.args).Count) {
        throw 'Client config must launch EXE without arguments.'
    }
    if ((Get-Content -LiteralPath $sentinel -Raw) -ne 'preserve me') {
        throw 'Unrelated file was changed.'
    }

    # A former plugin-owned file is removed on upgrade, unrelated files remain.
    $stateFile = Join-Path $target '.dnspy-mcp-install.json'
    $state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
    $obsolete = Join-Path $target 'obsolete-test.dll'
    [IO.File]::WriteAllText($obsolete, 'old owned file')
    $state.files = @($state.files) + @(@{
        name = 'obsolete-test.dll'
        sha256 = (Get-FileHash $obsolete).Hash
        owned = $true
    })
    $state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $stateFile -Encoding utf8
    Invoke-Install $PackageDir
    if (Test-Path -LiteralPath $obsolete) {
        throw 'Stale owned file was not removed.'
    }

    # Force a late write failure after a changed DLL was copied, then check rollback.
    $upgrade = Join-Path $testRoot 'upgrade-package'
    [void][IO.Directory]::CreateDirectory($upgrade)
    Get-ChildItem -LiteralPath $PackageDir -File | Copy-Item -Destination $upgrade
    $upgradeDll = Join-Path $upgrade 'dnSpy.MCP.Protocol.dll'
    $stream = [IO.File]::Open($upgradeDll, [IO.FileMode]::Append)
    try {
        $stream.WriteByte(0)
    } finally {
        $stream.Dispose()
    }
    $upgradeManifest = Get-Content (Join-Path $upgrade 'package.json') -Raw | ConvertFrom-Json
    ($upgradeManifest.files | Where-Object name -eq 'dnSpy.MCP.Protocol.dll').sha256 = (Get-FileHash $upgradeDll).Hash
    $upgradeManifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $upgrade 'package.json')
    $before = @{}
    foreach ($file in Get-ChildItem -LiteralPath $target -File -Force) {
        $before[$file.Name] = (Get-FileHash -LiteralPath $file.FullName).Hash
    }
    $lockedConfig = Get-Item -LiteralPath (Join-Path $target 'mcp-claude-code.json')
    $lockedConfig.IsReadOnly = $true
    try {
        Invoke-Install $upgrade $false
    } finally {
        (Get-Item -LiteralPath $lockedConfig.FullName).IsReadOnly = $false
    }
    foreach ($name in $before.Keys) {
        if ((Get-FileHash -LiteralPath (Join-Path $target $name)).Hash -ne $before[$name]) {
            throw "Rollback did not restore: $name"
        }
    }
    Write-Host '[test] Passed: failed upgrade restores the prior DLL and configurations.'

    # Hash/host/path errors must reject before modifying the installed files.
    $stateHash = (Get-FileHash -LiteralPath $stateFile).Hash
    $badPackage = Join-Path $testRoot 'bad-package'
    [void][IO.Directory]::CreateDirectory($badPackage)
    $bad = Get-Content (Join-Path $PackageDir 'package.json') -Raw | ConvertFrom-Json
    $bad.hostRuntime = if ($bad.hostRuntime -eq 'net48') {
        'net10'
    } else {
        'net48'
    }
    $bad | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $badPackage 'package.json')
    Invoke-Install $badPackage $false
    $bad.hostRuntime = $manifest.hostRuntime
    $bad.files[0].name = '..\outside.dll'
    $bad | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $badPackage 'package.json')
    Invoke-Install $badPackage $false
    $bad = Get-Content (Join-Path $PackageDir 'package.json') -Raw | ConvertFrom-Json
    $bad.files[0].sha256 = '0' * 64
    $bad | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $badPackage 'package.json')
    Invoke-Install $badPackage $false
    if ((Get-FileHash -LiteralPath $stateFile).Hash -ne $stateHash) {
        throw 'Rejected installation changed state.'
    }
    Write-Host '[test] Passed: PS5.1 install/reinstall, stale ownership, config, wrong host, traversal and checksum rejection.'

    # Test only the EXE in isolation, without adjacent plugin DLLs or dotnet on PATH.
    $solo = Join-Path $testRoot 'server-only'
    [void][IO.Directory]::CreateDirectory($solo)
    Copy-Item -LiteralPath (Join-Path $PackageDir 'dnSpy.MCP.Server.exe') -Destination $solo
    & (Join-Path $PSScriptRoot 'test-mcp.ps1') -ServerPath (Join-Path $solo 'dnSpy.MCP.Server.exe')
} finally {
    $env:PATH = $oldPath
    $env:DOTNET_ROOT = $oldRoot
    $env:DOTNET_ROOT_X64 = $oldRoot64
}

$global:LASTEXITCODE = 0
Write-Host "[test] Package tests passed. Fixtures retained at: $testRoot"
