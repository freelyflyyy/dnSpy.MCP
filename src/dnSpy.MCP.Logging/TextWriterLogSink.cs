using dnSpy.MCP.Logging.Extensions.Formatting;

namespace dnSpy.MCP.Logging {
    public sealed class TextWriterLogSink : ILogSink {
        private readonly TextWriter writer;
        private readonly ILogEntryFormatter formatter;

        public TextWriterLogSink(TextWriter writer, ILogEntryFormatter? formatter = null) {
            this.writer = TextWriter.Synchronized(
                writer ?? throw new ArgumentNullException(nameof(writer)));

            this.formatter = formatter ?? new SimpleTextFormatter();
        }

        public void Write(LogEntry entry) {
            if (entry is null) {
                throw new ArgumentNullException(nameof(entry));
            }
            writer.WriteLine(formatter.Format(entry));
        }
    }
}