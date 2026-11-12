using dnSpy.Contracts.Text;
using dnSpy.MCP.Logging;
using dnSpy.MCP.Logging.Extensions.Formatting;
using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Extension.Logging {
    internal sealed class DnSpyLogFormatter : ILogEntryFormatter {
        private readonly SimpleTextFormatter formatter = new SimpleTextFormatter();

        public string Format(LogEntry entry) =>
            formatter.Format(entry);

        public TextColor GetColor(LogLevel level) =>
            level switch {
                LogLevel.Warning => TextColor.Yellow,
                LogLevel.Error => TextColor.Error,
                LogLevel.Critical => TextColor.Error,
                _ => TextColor.Text
            };
    }
}
