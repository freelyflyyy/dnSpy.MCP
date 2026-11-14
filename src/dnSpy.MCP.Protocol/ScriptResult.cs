namespace dnSpy.MCP.Protocol {
    public sealed class ScriptResult {
        public bool Success { get; }
        public object? Value { get; }
        public ScriptError? Error { get; }
        public ScriptResult(bool Success, object? Value, ScriptError? Error) {
            this.Success = Success;
            this.Value = Value;
            this.Error = Error;
        }

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
