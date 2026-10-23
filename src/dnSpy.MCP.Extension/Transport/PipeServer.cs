using dnSpy.MCP.Extension.Scripting;
using dnSpy.MCP.Protocol;
using System.Buffers.Binary;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace dnSpy.MCP.Extension.Transport {
    [Export(typeof(PipeServer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class PipeServer {
        private readonly ScriptExecutor scripts;
        public string PipeName => PipeProtocol.GetPipeName(Environment.ProcessId);

        [ImportingConstructor]
        public PipeServer(ScriptExecutor scripts) {
            this.scripts = scripts;
        }

        public async Task RunAsync(CancellationToken stopToken) {
            while (!stopToken.IsCancellationRequested) {
                using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await pipe.WaitForConnectionAsync(stopToken);

                try {
                    await HandleClientAsync(pipe, stopToken);
                } catch (OperationCanceledException)
                    when (stopToken.IsCancellationRequested) {
                    return;
                } catch (OperationCanceledException) {
                    Trace.WriteLine("Client request timed out.");
                } catch (Exception ex)
                    when (ex is IOException or InvalidDataException) {

                    Trace.WriteLine($"Client request failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken stopToken) {
            // Read the request from the client with a timeout
            byte[] requestBytes;
            using (var receiveTimeout = CancellationTokenSource.CreateLinkedTokenSource(stopToken)) {
                receiveTimeout.CancelAfter(TimeSpan.FromSeconds(60));

                requestBytes = await PipeProtocol.ReadMessageAsync(pipe, PipeProtocol.MaxRequestSize, receiveTimeout.Token);
            }
            stopToken.ThrowIfCancellationRequested();

            // Process the request and execute the script
            ScriptResult result;
            try {
                var request = JsonSerializer.Deserialize<PipeRequest>(requestBytes);

                if (request is null || string.IsNullOrWhiteSpace(request.Code)) {
                    result = ScriptResult.Failure("invalid_request", "Code cannot be empty.");
                } else {
                    result = await scripts.ExecuteAsync(request.Code);
                }
            } catch (JsonException ex) {
                result = ScriptResult.Failure("invalid_request", ex.Message);
            } catch (Exception ex) {
                result = ScriptResult.Failure("host_error", $"{ex.GetType().Name}: {ex.Message}");
            }

            // Serialize the result and send it back to the client
            var responseBytes = JsonSerializer.SerializeToUtf8Bytes(result);
            if (responseBytes.Length > PipeProtocol.MaxResponseSize) {
                responseBytes = JsonSerializer.SerializeToUtf8Bytes(
                    ScriptResult.Failure("response_too_large", "The script executed, but the response exceeds 4 MiB."));
            }

            using var sendTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await PipeProtocol.WriteMessageAsync(
                pipe,
                responseBytes,
                PipeProtocol.MaxResponseSize,
                sendTimeout.Token);
        }
    }
}
