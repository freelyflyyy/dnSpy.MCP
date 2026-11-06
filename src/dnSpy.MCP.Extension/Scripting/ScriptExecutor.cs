using dnSpy.MCP.Extension.Api;
using dnSpy.MCP.Logging;
using dnSpy.MCP.Protocol;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Threading;

namespace dnSpy.MCP.Extension.Scripting {
    [Export(typeof(ScriptExecutor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ScriptExecutor {
        private readonly DnSpyContext context;
        private readonly ScriptOptions options;
        private readonly Dispatcher dispatcher;
        private readonly SemaphoreSlim executionGate = new(1, 1);

        [ImportingConstructor]
        public ScriptExecutor(DnSpyContext context) {
            this.context = context;

            dispatcher = System.Windows.Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("The WPF application is unavailable.");

            options = ScriptOptions.Default
                .AddReferences(typeof(ScriptGlobals).Assembly, typeof(System.Linq.Enumerable).Assembly)
                .AddImports("System", "System.Linq");
        }

        public Task<ScriptResult> ExecuteAsync(string code) {
            if (dispatcher.CheckAccess()) {
                return ExecuteQueuedAsync(code);
            }

            return dispatcher
                .InvokeAsync<Task<ScriptResult>>(
                    () => ExecuteQueuedAsync(code))
                .Task
                .Unwrap();
        }

        private async Task<ScriptResult> ExecuteQueuedAsync(string code) {
            dispatcher.VerifyAccess();

            await executionGate.WaitAsync();

            try {
                var watch = Stopwatch.StartNew();
                var result = await ExecuteCoreAsync(code);
                watch.Stop();

                string preview;

                try {
                    if (result.Success) {
                        preview = result.Value is JsonElement json
                            ? json.GetRawText()
                            : result.Value is null ? "null" : "<preview unavailable>";
                    } else {
                        preview = result.Error?.Message ?? "Unknown error";

                        if (result.Error?.Diagnostics is { Length: > 0 }) {
                            preview += "; " + result.Error.Diagnostics[0];
                        }
                    }
                } catch (Exception ex) {
                    preview = $"<preview unavailable: {ex.GetType().Name}>";
                }

                if (result.Success) {
                    Log.i(
                        "Script.Completed",
                        "script=" + code,
                        preview,
                        watch.ElapsedMilliseconds);
                } else {
                    Log.e(
                        "Script.Failed",
                        $"{result.Error?.Code}; script={code}",
                        preview,
                        watch.ElapsedMilliseconds);
                }

                return result;
            } finally {
                executionGate.Release();
            }
        }

        private async Task<ScriptResult> ExecuteCoreAsync(string code) {
            if (string.IsNullOrWhiteSpace(code)) {
                return ScriptResult.Failure("invalid_request", "Code is null or whitespace.");
            }

            object? value;
            try {
                var globals = new ScriptGlobals(context);
                value = await CSharpScript.EvaluateAsync(
                    code,
                    options,
                    globals : globals,
                    typeof(ScriptGlobals)
                );
            } catch (CompilationErrorException ex) {
                return ScriptResult.Failure(
                    "compilation_error",
                    "The script compilation failed.",
                    ex.Diagnostics.Select(item => item.ToString()).ToArray());
            } catch (Exception ex) {
                return ScriptResult.Failure(
                    "runtime_error",
                    $"{ex.GetType().Name}: {ex.Message}");
            }

            try {
                var jsonValue = JsonSerializer.SerializeToElement(value);
                return new ScriptResult(
                    Success: true,
                    Value: jsonValue,
                    Error: null);
            } catch(Exception ex) {
                return ScriptResult.Failure(
                    "serialization_error",
                    "The script has been executed, but the return value could not be converted to JSON:"
                    + $"{ex.GetType().Name}: {ex.Message}");
            };
        }
    }
}
