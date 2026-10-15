namespace dnSpy.MCP.Extension.Api {
    public sealed record MethodInfo(
        uint Token,
        string FullName,
        bool HasBody
    );
}
