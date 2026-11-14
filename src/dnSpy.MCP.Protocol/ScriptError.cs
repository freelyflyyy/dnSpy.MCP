namespace dnSpy.MCP.Protocol {
    public sealed class ScriptError {
        public string Code { get; }
        public string Message { get; }
        public string[] Diagnostics { get; }
        public ScriptError(string Code, string Message, string[] Diagnostics) {
            this.Code = Code;
            this.Message = Message;
            this.Diagnostics = Diagnostics;
        }
    }
}
