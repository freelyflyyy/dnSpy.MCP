using dnSpy.MCP.Protocol;
using dnSpy.MCP.Server.Sessions;
using dnSpy.MCP.Server.Transport;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using static dnSpy.MCP.Server.Transport.PipeClient;

namespace dnSpy.MCP.Server.Tools {
    [McpServerToolType]
    public sealed class SessionTools {
        private readonly SessionDiscovery discovery;
        private readonly PipeClient client;

        public SessionTools(SessionDiscovery discovery, PipeClient client) {
            this.discovery = discovery;
            this.client = client;
        }

        [McpServerTool(
            Name = "session",
            ReadOnly = true,
            Destructive = false,
            Idempotent = true,
            OpenWorld = false)]
        [Description(
            "Omit targetProcessId to list candidate dnSpy pipes without connecting. "
            + "Supply a PID to probe that host for up to two seconds. "
            + "If Status is ready, use Host.InstanceId as expectedInstanceId "
            + "to pin later scripts to the same instance. "
            + "An unresponsive host may be busy running a script.")]
        public async Task<string> ListSessionsAsync(
            CancellationToken cancellationToken,
            [Description("Optional positive process ID (PID) to handshake with. Omit to list all available candidate sessions.")]
            int? targetProcessId = null) {

            cancellationToken.ThrowIfCancellationRequested();

            if (targetProcessId is null) {
                try {
                    return JsonSerializer.Serialize(discovery.ListSessions());
                }
                catch (Exception ex) when (
                    ex is IOException or UnauthorizedAccessException
                        or PlatformNotSupportedException) {
                    throw new McpException($"session_discovery_error: {ex.Message}");
                }
            }

            int pid = targetProcessId.Value;

            if (pid <= 0) {
                throw new McpException("invalid_request: targetProcessId must be positive.");
            }

            try {
                var host = await client.HelloAsync(pid, cancellationToken);
                return Report(pid, "ready", host);
            } catch (TimeoutException ex) {
                return Report(pid, "unresponsive", detail: ex.Message);
            } catch (NotSupportedException ex) {
                return Report(pid, "incompatible", detail: ex.Message);
            } catch (UnauthorizedAccessException ex) {
                return Report(pid, "access_denied", detail: ex.Message);
            } catch (PipeRemoteException ex) {
                return Report(pid, "remote_error", detail: ex.Message);
            } catch (Exception ex) when (
                ex is JsonException or InvalidDataException or IOException) {
                return Report(pid, "probe_error", detail: ex.Message);
            }
        }

        private static string Report(int pid, string status, HostInfo? host = null, string? detail = null) =>
            JsonSerializer.Serialize(new {
                ProcessId = pid,
                PipeName = PipeProtocol.GetPipeName(pid),
                Status = status,
                ProtocolVersion = host is null ? (int?)null : PipeProtocol.Version,
                Host = host,
                Detail = detail
            });
    }
}
