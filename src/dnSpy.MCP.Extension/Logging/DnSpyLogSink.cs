using dnSpy.Contracts.Text;
using dnSpy.MCP.Extension.UI;
using dnSpy.MCP.Logging;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Logging {
    [Export(typeof(ILogSink))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class DnSpyLogSink : ILogSink {
        private readonly McpOutput output;

        [ImportingConstructor]
        public DnSpyLogSink(McpOutput output) {
            this.output = output;
        }

        public void Write(LogEntry entry) {
            var color = entry.Level switch {
                LogLevel.Warning => TextColor.Yellow,
                LogLevel.Error => TextColor.Error,
                _ => TextColor.Text
            };

            output.WriteLine(
                TextLogFormatter.Format(entry),
                color);
        }
    }
}