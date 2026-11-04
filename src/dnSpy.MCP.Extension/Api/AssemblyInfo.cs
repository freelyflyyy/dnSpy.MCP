namespace dnSpy.MCP.Extension.Api {
    public sealed class AssemblyInfo {
        public string FullName { get; }
        public string FilePath { get; }

        public AssemblyInfo(string FullName, string FilePath) {
            this.FullName = FullName;
            this.FilePath = FilePath;
        }
    }
}