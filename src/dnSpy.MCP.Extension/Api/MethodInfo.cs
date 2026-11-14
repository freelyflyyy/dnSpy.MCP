namespace dnSpy.MCP.Extension.Api {
    public sealed class MethodInfo {
        public uint Token { get; }
        public string FullName { get; }
        public bool HasBody { get; }

        public MethodInfo(uint Token, string FullName, bool HasBody) {
            this.Token = Token;
            this.FullName = FullName;
            this.HasBody = HasBody;
        }
    }
}