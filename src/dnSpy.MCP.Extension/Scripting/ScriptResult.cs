using System.Text.Json;

namespace dnSpy.MCP.Extension.Scripting {
    public sealed record ScriptResult(
        bool Success,
        JsonElement? Value,
        ScriptError? Error
    );
}
