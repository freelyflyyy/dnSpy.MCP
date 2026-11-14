using dnSpy.MCP.Logging.Extensions.Formatting;

namespace dnSpy.MCP.Logging.Extensions.Trace {
    public sealed class TraceLogSink : ILogSink {
        private readonly ILogEntryFormatter formatter;
        private readonly object gate = new object();

        public TraceLogSink(ILogEntryFormatter? formatter = null) {
            this.formatter = formatter ?? new SimpleTextFormatter();
        }

        public void Write(LogEntry entry) {
            if (entry is null) {
                throw new ArgumentNullException(nameof(entry));
            }

            string text;

            lock (gate) {
                text = formatter.Format(entry);
            }

            System.Diagnostics.Trace.WriteLine(text);
        }
    }
}
