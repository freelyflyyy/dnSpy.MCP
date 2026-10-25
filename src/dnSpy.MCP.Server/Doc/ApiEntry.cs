namespace dnSpy.MCP.Server.Doc {
    public sealed record ApiEntry(
        string Name,
        string Signature,
        string Description,
        string Returns,
        string Notes,
        string Example);
}
