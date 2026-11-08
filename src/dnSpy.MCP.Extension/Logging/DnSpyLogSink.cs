using dnSpy.Contracts.Text;
using dnSpy.MCP.Extension.UI;
using dnSpy.MCP.Logging;
using dnSpy.MCP.Logging.Extensions.Formatting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Logging {
    [Export(typeof(ILogSink))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class DnSpyLogSink : ILogSink {
        private readonly McpOutput output;
        private readonly ILogEntryFormatter formatter = new SimpleTextFormatter();

        [ImportingConstructor]
        public DnSpyLogSink(McpOutput output) {
            this.output = output;
        }

        public void Write(LogEntry entry) {
            var color = entry.Level switch {
                LogLevel.Warning => TextColor.Yellow,
                LogLevel.Error => TextColor.Error,
                LogLevel.Critical => TextColor.Error,
                _ => TextColor.Text
            };

            output.WriteLine(
                formatter.Format(entry),
                color);
        }
    }
}