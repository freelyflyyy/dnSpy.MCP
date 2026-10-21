param(
    [Parameter(Mandatory = $true)]
    [int]$TargetProcessId
)

$mcpPipeName = "dnSpy-Mcp-$TargetProcessId"

$mcpRequest = @{
    Code = 'return ctx.Documents.ListAssemblies();'
} | ConvertTo-Json -Compress

$mcpBytes = [System.Text.Encoding]::UTF8.GetBytes($mcpRequest)

$mcpClient = [System.IO.Pipes.NamedPipeClientStream]::new(
    '.',
    $mcpPipeName,
    [System.IO.Pipes.PipeDirection]::InOut
)

$mcpWriter = $null
$mcpReader = $null

try {
    $mcpClient.Connect(5000)

    $mcpWriter = [System.IO.BinaryWriter]::new(
        $mcpClient,
        [System.Text.Encoding]::UTF8,
        $true
    )

    $mcpReader = [System.IO.BinaryReader]::new(
        $mcpClient,
        [System.Text.Encoding]::UTF8,
        $true
    )

    $mcpWriter.Write([int]$mcpBytes.Length)
    $mcpWriter.Write($mcpBytes)
    $mcpWriter.Flush()

    $mcpLength = $mcpReader.ReadInt32()

    if ($mcpLength -le 0 -or $mcpLength -gt 4MB) {
        throw 'Invalid response length.'
    }

    $mcpBody = $mcpReader.ReadBytes($mcpLength)

    if ($mcpBody.Length -ne $mcpLength) {
        throw 'Incomplete response.'
    }

    [System.Text.Encoding]::UTF8.GetString($mcpBody)
}
finally {
    if ($null -ne $mcpWriter) {
        $mcpWriter.Dispose()
    }

    if ($null -ne $mcpReader) {
        $mcpReader.Dispose()
    }

    $mcpClient.Dispose()
}
