namespace dnSpy.MCP.Logging {
    public sealed class LogEntry {
        public DateTimeOffset Timestamp { get; }
        public LogLevel Level { get; }
        public string EventName { get; }
        public string Message { get; }
        public string? ResultPreview { get; }
        public long? ElapsedMs { get; }
        public bool MessageTruncated { get; }
        public bool ResultTruncated { get; }

        internal LogEntry(
            DateTimeOffset Timestamp,
            LogLevel Level,
            string EventName,
            string Message,
            string? ResultPreview = null,
            long? ElapsedMs = null,
            bool MessageTruncated = false,
            bool ResultTruncated = false) {
            this.Timestamp = Timestamp;
            this.Level = Level;
            this.EventName = EventName;
            this.Message = Message;
            this.ResultPreview = ResultPreview;
            this.ElapsedMs = ElapsedMs;
            this.MessageTruncated = MessageTruncated;
            this.ResultTruncated = ResultTruncated;
        }
    }
}
