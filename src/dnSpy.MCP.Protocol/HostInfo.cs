namespace dnSpy.MCP.Protocol {
    public sealed class HostInfo {
        public int ProcessId { get; }
        public Guid InstanceId { get; }
        public string HostRuntime { get; }
        public string PluginVersion { get; }

        public HostInfo(int ProcessId, Guid InstanceId, string HostRuntime, string PluginVersion) {
            this.ProcessId = ProcessId;
            this.InstanceId = InstanceId;
            this.HostRuntime = HostRuntime;
            this.PluginVersion = PluginVersion;
        }
    }
}
