using dnSpy.MCP.Protocol;
using ModelContextProtocol.Protocol;
using System.IO.Pipes;
using System.Text.Json;

namespace dnSpy.MCP.Server.Transport {
    public sealed class PipeClient {
        public async Task<HostInfo> HelloAsync(
            int targetProcessId,
            CancellationToken cancellationToken = default) {

            using var timeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            try {
                var host = await ExchangeAsync<HostInfo>(
                    targetProcessId, PipeProtocol.Hello, null, null, timeout.Token);

                if (host.ProcessId != targetProcessId ||
                    host.InstanceId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(host.HostRuntime) ||
                    string.IsNullOrWhiteSpace(host.PluginVersion)) {
                    throw new InvalidDataException("invalid_host_info");
                }

                return host;
            } catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested) {
                throw new TimeoutException("No handshake response within two seconds.");
            }
        }

        public async Task<ScriptResult> ExecuteAsync(
            int targetProcessId,
            string code,
            CancellationToken cancellationToken = default,
            Guid? expectedInstanceId = null) {

            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            if (expectedInstanceId == Guid.Empty) {
                throw new ArgumentException(
                    "Instance ID must not be empty.", nameof(expectedInstanceId));
            }

            var instanceId = expectedInstanceId
                ?? (await HelloAsync(targetProcessId, cancellationToken)).InstanceId;

            return await ExchangeAsync<ScriptResult>(
                targetProcessId, PipeProtocol.Execute, code,
                instanceId, cancellationToken);
        }

        private static async Task<T> ExchangeAsync<T>(
            int pid,
            string operation,
            string? script,
            Guid? intanceId,
            CancellationToken token) where T : class {
            var pipeName = PipeProtocol.GetPipeName(pid);
            var id = Guid.NewGuid();
            var request = new PipeRequest(
                PipeProtocol.Version,
                id,
                operation,
                script,
                intanceId);
            var data = JsonSerializer.SerializeToUtf8Bytes(request);

            using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            await pipe.ConnectAsync(5000, token);
            await PipeProtocol.WriteMessageAsync(
                pipe, data, PipeProtocol.MaxRequestSize, token);

            var bytes = await PipeProtocol.ReadMessageAsync(
                pipe, PipeProtocol.MaxResponseSize, token);

            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object) {
                throw new InvalidDataException("Invalid_response");
            }

            if (!root.TryGetProperty("ProtocolVersion", out var version) ||
                version.ValueKind != JsonValueKind.Number ||
                !version.TryGetInt32(out var actualVersion) ||
                actualVersion != PipeProtocol.Version) {
                throw new NotSupportedException("incompatible_protocol: update the Server and plugin together.");
            }

            if (!root.TryGetProperty("RequestId", out var correlation) ||
                correlation.ValueKind != JsonValueKind.String ||
                !correlation.TryGetGuid(out var actualId) ||
                actualId != id) {
                throw new InvalidDataException("request_id_mismatch");
            }

            var reply = root.Deserialize<PipeResponse<T>>()
                ?? throw new InvalidDataException("empty_response");

            if (reply.Error is not null) {
                if (reply.Result is not null || string.IsNullOrWhiteSpace(reply.Error)) {
                    throw new InvalidDataException("invalid_response");
                }

                if (reply.Error == "incompatible_protocol") {
                    throw new NotSupportedException(reply.Error);
                }

                throw new PipeRemoteException(id, reply.Error);
            }

            return reply.Result ?? throw new InvalidDataException("missing_result");
        }

        internal sealed class PipeRemoteException : IOException {
            public Guid RequestId { get; }
            public string Code { get; }

            public PipeRemoteException(Guid RequestId, string Code)
                : base($"{Code} (request {RequestId})") {
                this.RequestId = RequestId;
                this.Code = Code;
            }
        }
    }
}
