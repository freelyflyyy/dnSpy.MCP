using dnSpy.MCP.Server.Transport;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace dnSpy.MCP.Server.Tools {
    [McpServerToolType]
    public sealed class ScriptTools {
        private readonly PipeClient client;

        public ScriptTools(PipeClient client) {
            this.client = client;
        }

        [McpServerTool(
            Name = "execute_csharp",
            ReadOnly = false,
            Destructive = true,
            Idempotent = false,
            OpenWorld = true)]
        [Description(
            "Execute C# in a running dnSpy instance with its pipe service enabled. "
            + "Globals: ctx.Documents and ctx.Decompiler. "
            + "Use return to produce a JSON-serializable value. "
            + "Cancellation stops waiting, not remote execution. "
            + "Do not automatically retry after transport failure."
            + "Use search_api to look up ctx API signatures and examples. ")]
        public async Task<CallToolResult> ExecuteCSharpAsync(
            [Description("The target process ID.")]
            int targetProcessId,
            [Description("C# script, for example: return ctx.Documents.ListAssemblies();")]
            string code,
            CancellationToken cancellationToken) {

            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeout.CancelAfter(TimeSpan.FromMinutes(2));

            try {
                var result = await client.ExecuteAsync(
                    targetProcessId,
                    code,
                    requestTimeout.Token);

                return CreateToolResult(
                    JsonSerializer.Serialize(result),
                    isError: !result.Success);
            } catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (OperationCanceledException) {
                return CreateToolResult(
                   "request_timeout: Stopped waiting for dnSpy. "
                   + "The script may still be running. Do not automatically retry.",
                   isError: true);
            } catch (Exception ex) {
                return CreateToolResult(
                    $"transport_error: {ex.GetType().Name}: {ex.Message} "
                    + "If the request was sent, the script may still be running. "
                    + "Do not automatically retry.",
                    isError: true);
            }
        }

        private static CallToolResult CreateToolResult(
            string text,
            bool isError) {

            return new CallToolResult {
                IsError = isError,
                Content = [
                    new TextContentBlock {
                        Text = text
                    }
                ]
            };
        }
    }
}
