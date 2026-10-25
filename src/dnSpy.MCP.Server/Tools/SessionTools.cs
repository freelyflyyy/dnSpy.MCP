using dnSpy.MCP.Server.Sessions;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace dnSpy.MCP.Server.Tools {
    [McpServerToolType]
    public sealed class SessionTools {
        private readonly SessionDiscovery discovery;

        public SessionTools(SessionDiscovery discovery) {
            this.discovery = discovery;
        }

        [McpServerTool(
            Name = "session",
            ReadOnly = true,
            Destructive = false,
            Idempotent = true,
            OpenWorld = false)]
        [Description(
            "List candidate dnSpy sessions discovered from local Windows pipe names. "
            + "ProcessId is parsed from the pipe name. "
            + "This does not verify process identity, connection permissions, "
            + "availability or responsiveness. "
            + "Use a returned ProcessId as targetProcessId for execute_csharp.")]
        public string ListSessions() {
            try {
                return JsonSerializer.Serialize(
                    discovery.ListSessions());
            } catch (Exception ex)
                  when (ex is IOException
                      or UnauthorizedAccessException
                      or PlatformNotSupportedException) {

                throw new McpException(
                    $"session_discovery_error: {ex.Message}");
            }
        }
    }
}
