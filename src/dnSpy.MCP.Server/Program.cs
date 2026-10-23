using dnSpy.MCP.Protocol;
using dnSpy.MCP.Server.Tools;
using dnSpy.MCP.Server.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

if (args.Length == 0) {
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options => {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Services.AddSingleton<PipeClient>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<ScriptTools>();

    await builder.Build().RunAsync();
    return 0;
}

if (args.Length != 2 ||
    !int.TryParse(args[0], out var targetProcessId) ||
    targetProcessId <= 0) {

    Console.Error.WriteLine(
        "Usage: dnSpy.MCP.Server <process-id> <script.csx>");

    return 2;
}

using var requestCancellation =
    new CancellationTokenSource(TimeSpan.FromMinutes(2));

Console.CancelKeyPress += (_, eventArgs) => {
    eventArgs.Cancel = true;
    requestCancellation.Cancel();
};

try {
    var code = await File.ReadAllTextAsync(
        args[1],
        requestCancellation.Token);

    var client = new PipeClient();

    var result = await client.ExecuteAsync(
        targetProcessId,
        code,
        requestCancellation.Token);

    Console.WriteLine(
        JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions {
                WriteIndented = true
            }));

    return result.Success ? 0 : 1;
} catch (OperationCanceledException) {
    Console.Error.WriteLine(
        "Request canceled or timed out. "
        + "A script already sent to dnSpy may still be running.");

    return 2;
} catch (Exception ex) {
    Console.Error.WriteLine(
        $"{ex.GetType().Name}: {ex.Message}");

    return 2;
}