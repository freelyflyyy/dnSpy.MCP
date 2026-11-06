namespace dnSpy.MCP.Logging {
    public sealed class TextWriterLogSink : ILogSink {
        private readonly TextWriter writer;

        public TextWriterLogSink(TextWriter writer) {
            this.writer = TextWriter.Synchronized(
                writer ?? throw new ArgumentNullException(nameof(writer)));
        }

        public void Write(LogEntry entry) =>
            writer.WriteLine(TextLogFormatter.Format(entry));
    }
}