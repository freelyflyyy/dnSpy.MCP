namespace dnSpy.MCP.Extension.Scripting {
    public sealed record ScriptError(
        string Code,
        string Message,
        string[] Diagnostics
    );
}
