using dnSpy.MCP.Logging.Extensions.Formatting;

namespace dnSpy.MCP.Logging.Extensions.TextWriter {
    public sealed class TextWriterSinkOptions {
        public ILogEntryFormatter Formatter { get; set; } = new SimpleTextFormatter();

        public bool AutoFlush { get; set; } = true;
    }
}
