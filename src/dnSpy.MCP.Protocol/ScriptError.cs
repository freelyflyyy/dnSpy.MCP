namespace dnSpy.MCP.Protocol {
    public sealed record ScriptError(
        string Code,
        string Message,
        string[] Diagnostics
    );
}
