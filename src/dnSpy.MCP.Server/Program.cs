using dnSpy.MCP.Logging;
using dnSpy.MCP.Logging.Extensions.TextWriter;
using dnSpy.MCP.Logging.Extensions.Console;
using dnSpy.MCP.Server.Doc;
using dnSpy.MCP.Server.Sessions;
using dnSpy.MCP.Server.Tools;
using dnSpy.MCP.Server.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

if (args.Length == 0) {
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();

    var configuration = new LogConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogLevel.Warning)
        .WriteTo.Console();

    configuration.ApplyTo(builder.Logging);

    builder.Services.AddSingleton<PipeClient>();
    builder.Services.AddSingleton<SessionDiscovery>();
    builder.Services.AddSingleton<ApiCatalog>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<ScriptTools>()
        .WithTools<SessionTools>()
        .WithTools<ApiTools>();

    using var host = builder.Build();
    Log.Initialize(host.Services.GetRequiredService<ILoggerFactory>());

    try {
        await host.StartAsync();
        await host.WaitForShutdownAsync();
        return 0;
    } finally {
        Log.CloseAndFlush();
    }
}

Log.Initialize(new LogConfiguration()
    .MinimumLevel.Information()
    .WriteTo.TextWriter(Console.Error));

try {
    if (args.Length != 2 ||
        !int.TryParse(args[0], out var targetProcessId) ||
        targetProcessId <= 0) {

        Log.E<Program>("CLI.InvalidArguments",
            "Usage: dnSpy.MCP.Server <process-id> <script.csx>");

        return 2;
    }

    using var requestCancellation =
        new CancellationTokenSource(TimeSpan.FromMinutes(2));

    ConsoleCancelEventHandler cancelHandler = (_, eventArgs) => {
        eventArgs.Cancel = true;
        requestCancellation.Cancel();
    };

    Console.CancelKeyPress += cancelHandler;

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
        Log.W<Program>("Client.WaitEnded",
            "Request canceled or timed out. A submitted script may still be running.");
        return 2;
    } catch (Exception ex) {
        Log.E<Program>("Client.Failed", ex,
            "Request to process {ProcessId} failed.", targetProcessId);
        return 2;
    } finally {
        Console.CancelKeyPress -= cancelHandler;
    }
} finally {
    Log.CloseAndFlush();
}
