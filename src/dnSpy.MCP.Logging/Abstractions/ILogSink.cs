namespace dnSpy.MCP.Logging {
    public interface ILogSink {
        void Write(LogEntry entry);
    }
}
