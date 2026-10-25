using dnSpy.MCP.Protocol;
using System.Diagnostics;

namespace dnSpy.MCP.Server.Sessions {
    public sealed class SessionDiscovery {

        public SessionInfo[] ListSessions() {
            if (!OperatingSystem.IsWindows()) {
                throw new PlatformNotSupportedException("Session discovery currently supports Windows.");
            }

            var prefix = PipeProtocol.PipeNamePrefix;
            var sessions = new List<SessionInfo>();

            var paths = Directory.GetFiles(
                @"\\.\pipe\", $"{prefix}*");

            foreach (var path in paths) {
                var pipeName = Path.GetFileName(path);

                if (!pipeName.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase)) {

                    continue;
                }

                if (!int.TryParse(
                    pipeName.AsSpan(prefix.Length),
                    out var processId) ||
                    processId <= 0) {

                    continue;
                }

                var expectedName = PipeProtocol.GetPipeName(processId);

                if (!string.Equals(
                    pipeName,
                    expectedName,
                    StringComparison.OrdinalIgnoreCase)) {

                    continue;
                }

                sessions.Add(new SessionInfo(processId, pipeName));
            }
            return sessions
                    .DistinctBy(session => session.ProcessId)
                    .OrderBy(session => session.ProcessId)
                    .ToArray();
        }
    }
}
