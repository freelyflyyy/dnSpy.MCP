using System.Text.Json;

namespace dnSpy.MCP.Extension.Scripting {
    public sealed record ScriptResult(
        bool Success,
        JsonElement? Value,
        ScriptError? Error) {

        public static ScriptResult Failure(
            string code,
            string message,
            params string[] diagnostics) {

            return new ScriptResult(
                Success: false,
                Value: null,
                Error: new ScriptError(code, message, diagnostics));
        }
    }
}
