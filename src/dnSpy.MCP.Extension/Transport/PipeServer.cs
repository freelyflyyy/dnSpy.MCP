using dnSpy.MCP.Extension.Scripting;
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
        private const int MaxRequestSize = 256 * 1024; // 256 KB
        private const int MaxResponseSize = 4 * 1024 * 1024; // 4 MB
        private readonly ScriptExecutor scripts;
        public string PipeName => $"dnSpy-Mcp-{Environment.ProcessId}";

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

                try {
                    await pipe.WaitForConnectionAsync(stopToken);

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

                requestBytes = await ReadRequestAsync(pipe, receiveTimeout.Token);
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
            if (responseBytes.Length > MaxResponseSize) {
                responseBytes = JsonSerializer.SerializeToUtf8Bytes(
                    ScriptResult.Failure("response_too_large", "The script executed, but the response exceeds 4 MiB."));
            }

            using var sendTimeout = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            sendTimeout.CancelAfter(TimeSpan.FromSeconds(30));

            var header = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(header, responseBytes.Length);

            await pipe.WriteAsync(header, sendTimeout.Token);
            await pipe.WriteAsync(responseBytes, sendTimeout.Token);
        }


        private static async Task<byte[]> ReadRequestAsync(Stream stream, CancellationToken cancellationToken) {
            var header = new byte[4];
            await stream.ReadExactlyAsync(header, cancellationToken);

            var length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length <= 0 || length > MaxRequestSize) {
                throw new InvalidDataException(
                    "Request size must be between 1 byte and 256 KiB.");
            }

            var payload = new byte[length];
            await stream.ReadExactlyAsync(payload, cancellationToken);

            return payload;
        }
    }
}
