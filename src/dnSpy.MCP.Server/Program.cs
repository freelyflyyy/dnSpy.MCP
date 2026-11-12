using dnSpy.MCP.Logging;
using dnSpy.MCP.Logging.Extensions.TextWriter;
using dnSpy.MCP.Server.Doc;
using dnSpy.MCP.Server.Sessions;
using dnSpy.MCP.Server.Tools;
using dnSpy.MCP.Server.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

Log.Initialize(new TextWriterLogSink(Console.Error));

if (args.Length == 0) {
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(
            LogLevel.Warning);
    builder.Logging.AddConsole(options => {
        options.LogToStandardErrorThreshold =

        LogLevel.Trace;
    });

    builder.Services.AddSingleton<PipeClient>();
    builder.Services.AddSingleton<SessionDiscovery>();
    builder.Services.AddSingleton<ApiCatalog>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<ScriptTools>()
        .WithTools<SessionTools>()
        .WithTools<ApiTools>();

    await builder.Build().RunAsync();
    return 0;
}

if (args.Length != 2 ||
    !int.TryParse(args[0], out var targetProcessId) ||
    targetProcessId <= 0) {

    Log.e("CLI.InvalidArguments",
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
    Log.w("Client.WaitEnded",
        "Request canceled or timed out. A submitted script may still be running.");
    return 2;
} catch (Exception ex) {
    Log.e("Client.Failed", $"{ex.GetType().Name}: {ex.Message}");
    return 2;
}