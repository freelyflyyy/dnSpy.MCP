using dnSpy.MCP.Logging.Extensions.Formatting;
using System.Text;

namespace dnSpy.MCP.Logging.Extensions.Stream {
    public sealed class StreamSinkOptions {
        public Encoding Encoding { get; set; } = new UTF8Encoding(false);
        public int BufferSize { get; set; } = 1024;
        public ILogEntryFormatter Formatter { get; set; } = new SimpleTextFormatter();
        public bool AutoFlush { get; set; } = true;
    }
}
