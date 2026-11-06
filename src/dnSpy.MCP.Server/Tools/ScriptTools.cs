using dnSpy.MCP.Server.Transport;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using dnSpy.MCP.Logging;
using static dnSpy.MCP.Server.Transport.PipeClient;

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
            "Run C# inside the selected dnSpy process. "
            + "Pass expectedInstanceId to target a previously selected host; "
            + "if omitted, the server probes the current host first. "
            + "Use ctx.Documents or ctx.Decompiler and return a JSON-serializable value. "
            + "Use search_api for unfamiliar ctx methods. "
            + "The client waits up to two minutes; cancellation or timeout does not stop a submitted script. "
            + "Do not retry automatically. After instance_mismatch, verify the intended target before executing again.")]
        public async Task<CallToolResult> ExecuteCSharpAsync(
            [Description("Target dnSpy process ID (PID).")]
            int targetProcessId,
            [Description("C# script body, e.g.: return ctx.Documents.ListAssemblies();")]
            string code,
            CancellationToken cancellationToken,
            [Description("Host InstanceId from session(targetProcessId). Pass this to pin execution to the verified dnSpy instance and avoid redundant handshakes.")]
            Guid? expectedInstanceId = null) {

            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeout.CancelAfter(TimeSpan.FromMinutes(2));

            try {
                var result = await client.ExecuteAsync(
                    targetProcessId,
                    code,
                    requestTimeout.Token,
                    expectedInstanceId);

                return CreateToolResult(
                    JsonSerializer.Serialize(result),
                    isError: !result.Success);
            } catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested) {
                Log.w("Client.WaitCancelled", "Stopped waiting; a submitted script may still be running.");
                throw;
            } catch (OperationCanceledException) {
                Log.w("Client.WaitTimedOut", "Stopped waiting; a submitted script may still be running.");
                return CreateToolResult(
                   "request_timeout: Stopped waiting for dnSpy. "
                   + "The script may still be running. Do not automatically retry.",
                   isError: true);
            } catch (PipeRemoteException ex) {
                Log.w("Client.RemoteError", ex.Message);
                return CreateToolResult(
                    $"dnspy_error: {ex.Message}. Do not retry automatically.",
                    isError: true);
            } catch (NotSupportedException ex) {
                Log.w("Client.ProtocolMismatch", ex.Message);
                return CreateToolResult(ex.Message, isError: true);
            } catch (Exception ex) {
                Log.e("Client.TransportFailed", ex.ToString());
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
