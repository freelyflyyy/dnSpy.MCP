using dnSpy.MCP.Protocol;
using System.IO.Pipes;
using System.Text.Json;

namespace dnSpy.MCP.Server.Transport {
    public sealed class PipeClient {
        public async Task<ScriptResult> ExecuteAsync(
            int targetProcessId,
            string code,
            CancellationToken cancellationToken = default) {

            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var pipeName = PipeProtocol.GetPipeName(targetProcessId);
            var requestBytes = JsonSerializer.SerializeToUtf8Bytes(new PipeRequest(code));

            using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            await pipe.ConnectAsync(5000, cancellationToken);

            await PipeProtocol.WriteMessageAsync(pipe, requestBytes, PipeProtocol.MaxRequestSize, cancellationToken);
            var responseBytes = await PipeProtocol.ReadMessageAsync(
               pipe,
               PipeProtocol.MaxResponseSize,
               cancellationToken);

            return JsonSerializer.Deserialize<ScriptResult>(responseBytes)
                ?? throw new InvalidDataException(
                    "The server returned a null response.");
        }
    }
}
