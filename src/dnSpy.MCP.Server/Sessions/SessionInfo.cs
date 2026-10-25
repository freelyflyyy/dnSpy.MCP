namespace dnSpy.MCP.Server.Sessions {
    public sealed record SessionInfo(
        int ProcessId,
        string PipeName);
}
