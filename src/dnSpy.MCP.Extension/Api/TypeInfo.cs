namespace dnSpy.MCP.Extension.Api {
    public sealed class TypeInfo {
        public string ModuleName { get; }
        public uint Token { get; }
        public string FullName { get; }
        public int MethodCount { get; }

        public TypeInfo(string ModuleName, uint Token, string FullName, int MethodCount) {
            this.ModuleName = ModuleName;
            this.Token = Token;
            this.FullName = FullName;
            this.MethodCount = MethodCount;
        }
    }
}
