using dnSpy.MCP.Extension.Scripting;
using dnSpy.MCP.Logging;
using dnSpy.MCP.Protocol;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace dnSpy.MCP.Extension.Transport {
    [Export(typeof(PipeServer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class PipeServer {
        private readonly HostInfo hostInfo;
        private readonly ScriptExecutor scripts;
        public string PipeName { get; }

        [ImportingConstructor]
        public PipeServer(ScriptExecutor scripts) {
            this.scripts = scripts;

            using var process = Process.GetCurrentProcess();
            PipeName = PipeProtocol.GetPipeName(process.Id);
            this.hostInfo = new HostInfo(
                process.Id,
                Guid.NewGuid(),
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                typeof(PipeServer).Assembly.GetName().Version?.ToString() ?? "unknown"
            );
        }

        public async Task RunAsync(CancellationToken stopToken, Action onListenCallback) {
            bool listeningReported = false;
            while (!stopToken.IsCancellationRequested) {
                using var pipe = NamedPipeFactory.Create(PipeName);

                if (!listeningReported) {
                    listeningReported = true;
                    onListenCallback();
                }

                await pipe.WaitForConnectionAsync(stopToken);

                try {
                    await HandleClientAsync(pipe, stopToken);
                } catch (OperationCanceledException)
                    when (stopToken.IsCancellationRequested) {
                    return;
                } catch (OperationCanceledException) {
                    Log.w("Transport.ReceiveTimeout", "The client did not finish sending its request.");
                } catch (Exception ex)
                    when (ex is IOException or InvalidDataException) {

                    Log.e("Transport.RequestFailed", $"{ex.GetType().Name}: {ex.Message}");
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

            var responseBytes = await DispatchAsync(requestBytes);

            using var sendTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await PipeProtocol.WriteMessageAsync(
                pipe,
                responseBytes,
                PipeProtocol.MaxResponseSize,
                sendTimeout.Token);
        }
        private async Task<byte[]> DispatchAsync(byte[] bytes) {
            PipeRequest? request;

            try {
                request = JsonSerializer.Deserialize<PipeRequest>(bytes);
            } catch (JsonException) {
                return Reject(Guid.Empty, "invalid_request");
            }

            if(request is null) {
                return Reject(Guid.Empty, "invalid_request");
            }

            var id = request.RequestId;

            if (request.ProtocolVersion != PipeProtocol.Version) {
                return Reject(id, "incompatible_protocol");
            }

            if (id == Guid.Empty) {
                return Reject(id, "invalid_request");
            }

            switch (request.Operation) {
                case PipeProtocol.Hello:
                    if (request.Script is not null ||
                        request.ExpectedInstanceId.HasValue) {
                        return Reject(id, "invalid_request");
                    }
                    return Reply(id, hostInfo);
                    case PipeProtocol.Execute:
                        if(request.ExpectedInstanceId != hostInfo.InstanceId) {
                            return Reject(id, "instance_mismatch");
                        }
                    if (string.IsNullOrWhiteSpace(request.Script)) {
                        return Reject(id, "invalid_request");
                    }

                    try {
                        return Reply(id, await scripts.ExecuteAsync(request.Script!));
                    } catch (Exception ex) {
                    Log.e("Script.HostError", ex.ToString());
                    return Reject(id, "host_error");
                    }

                default:
                    return Reject(id, "unknown_operation");
            }
        }
        private static byte[] Reply<T>(Guid id, T value) where T : class {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(
                new PipeResponse<T>(PipeProtocol.Version, id, value, null));

            return bytes.Length <= PipeProtocol.MaxResponseSize
                ? bytes
                : Reject(id, "response_too_large");
        }
        private static byte[] Reject(Guid id, string error) =>
            JsonSerializer.SerializeToUtf8Bytes(
                new PipeResponse<object>(
                    PipeProtocol.Version,
                    id,
                    Result: null,
                    Error: error));
    }
}
