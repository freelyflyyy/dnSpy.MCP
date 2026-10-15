
namespace dnSpy.MCP.Extension.Api {
    public sealed record TypeInfo(
        string ModuleName,
        uint Token,
        string FullName,
        int MethodCount
    );
}
