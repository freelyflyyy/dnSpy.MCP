#Requires -Version 7.2

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $ServerPath
)
$ErrorActionPreference = 'Stop'

$serverFile = Get-Item -LiteralPath $ServerPath
if ($serverFile.PSIsContainer -or $serverFile.Extension -ne '.dll') {
    <# Action to perform if the condition is true #>
    throw "Expected a server DLL: $ServerPath"
}

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = (Get-Command dotnet -CommandType Application).Source
$startInfo.ArgumentList.Add($serverFile.FullName)
$startInfo.WorkingDirectory = $serverFile.DirectoryName
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardInput = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
$startInfo.StandardInputEncoding = [System.Text.UTF8Encoding]::new($false)

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$started = $false

function Send-McpMessage{
    param([hashtable]$Message)

    $json = ConvertTo-Json -InputObject $Message -Depth 20 -Compress
    $process.StandardInput.WriteLine($json)
    $process.StandardInput.Flush()
}

function Invoke-McpRequest {
    param (
        [int]$Id,
        [string]$Method,
        [hashtable]$Parameters = @{}
    )

    Send-McpMessage @{
        jsonrpc = '2.0'
        id = $Id
        method = $Method
        params = $Parameters
    }

    $timer = [System.Diagnostics.Stopwatch]::StartNew()

    while ($true) {
        $remaining = [TimeSpan]::FromSeconds(30) - $timer.Elapsed

        if ($remaining -le [TimeSpan]::Zero) {
            <# Action to perform if the condition is true #>
            throw "MCP request timed out: $Method"
        }

        $readTask = $process.StandardOutput.ReadLineAsync()
        $line = $readTask.WaitAsync($remaining).GetAwaiter().GetResult()

        if ($null -eq $line) {
            throw "Server closed stdout while waiting for: $Method"
        }

        $message = ConvertFrom-Json -InputObject $line -AsHashtable
        if ($message -isnot [System.Collections.IDictionary] -or
            $message['jsonrpc'] -ne '2.0') {
            throw "Invalid MCP message: $line"
        }

        # Notifications may arrive before the response.
        if (-not $message.ContainsKey('id') -and
            $message['method'] -like 'notifications/*') {
            continue
        }

        if (-not $message.ContainsKey('id') -or $message['id'] -ne $Id) {
            throw "Unexpected MCP response: $line"
        }

        if ($message.ContainsKey('error')) {
            throw "MCP request returned an error: $line"
        }

        if (-not $message.ContainsKey('result')) {
            throw "MCP response has no result: $line"
        }

        return $message['result']
    }
}

try {
    Write-Host '[test] [1/5] Starting packaged MCP server...'

    $started = $process.Start()

    if (-not $started) {
        throw 'Failed to start the MCP server.'
    }
    Write-Host '[test] [2/5] Checking MCP initialization...'

    $initialization = Invoke-McpRequest -Id 1 -Method 'initialize' -Parameters @{
        protocolVersion = '2025-06-18'
        capabilities = @{}
        clientInfo = @{
            name = 'dnSpy.MCP.SmokeTest'
            version = '1.0.0'
        }
    }

    if ([string]::IsNullOrWhiteSpace($initialization.protocolVersion)) {
        throw "Server returned an empty protocol version."
    }

    if ($null -eq $initialization.capabilities.tools) {
        throw 'Server did not advertise the tools capability.'
    }

    Send-McpMessage @{
        jsonrpc = '2.0'
        method = 'notifications/initialized'
    }

    Write-Host '[test] [3/5] Checking registered tools...'

    $result = Invoke-McpRequest -Id 2 -Method 'tools/list'
    $toolNames = @($result.tools | ForEach-Object { $_.name })

    foreach ($expected in @('session', 'search_api', 'execute_csharp')) {
        if ($toolNames -cnotcontains $expected) {
            throw "Expected MCP tool was not registered: $expected"
        }
    }

    Write-Host '[test] [4/5] Checking search_api results...'

    $apiName = 'ctx.Documents.ListMethods'
    $searchResult = Invoke-McpRequest -Id 3 -Method 'tools/call' -Parameters @{
        name = 'search_api'
        arguments = @{
            query = $apiName
            limit = 1
        }
    }

     if ($searchResult.isError -eq $true) {
        throw 'search_api returned an error for a valid query.'
    }

    $textBlocks = @(
        $searchResult.content | Where-Object { $_.type -ceq 'text' }
    )

    if ($textBlocks.Count -ne 1) {
        throw 'Expected one text result from search_api.'
    }

    $entries = ConvertFrom-Json -InputObject $textBlocks[0].text -AsHashtable -NoEnumerate

    if ($entries -isnot [array] -or
        $entries.Count -ne 1 -or
        $entries[0].Name -cne $apiName) {
        throw 'search_api did not return the expected API entry.'
    }

    Write-Host '[test] [5/5] Checking search_api invalid input...'

    $invalidResult = Invoke-McpRequest -Id 4 -Method 'tools/call' -Parameters @{
        name = 'search_api'
        arguments = @{
            query = $apiName
            limit = 0
        }
    }

    if ($invalidResult.isError -ne $true) {
        throw 'search_api did not reject limit = 0.'
    }

    $errorText = (
        $invalidResult.content |
            Where-Object { $_.type -ceq 'text' } |
            ForEach-Object { $_.text }
    ) -join "`n"

    if ($errorText -notlike '*invalid_request:*') {
        throw 'search_api did not return the expected invalid_request error.'
    }

} finally {
   if ($started) {
        $process.StandardInput.Close()

        if (-not $process.WaitForExit(5000)) {
            $process.Kill($true)
            $process.WaitForExit(5000) | Out-Null
        }
    }

    $process.Dispose()
}

Write-Host '[test] Passed: MCP initialization, tool registration, and search_api checks.'