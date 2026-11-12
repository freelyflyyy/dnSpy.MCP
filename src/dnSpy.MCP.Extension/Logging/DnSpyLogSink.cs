using dnSpy.Contracts.Text;
using dnSpy.MCP.Extension.UI;
using dnSpy.MCP.Logging;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Logging {
    [Export(typeof(ILogSink))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class DnSpyLogSink : ILogSink {
        private readonly McpOutput output;
        private readonly DnSpyLogFormatter formatter = new DnSpyLogFormatter();

        [ImportingConstructor]
        public DnSpyLogSink(McpOutput output) {
            this.output = output;
        }

        public void Write(LogEntry entry) {
            if (entry is null) {
                throw new ArgumentNullException(nameof(entry));
            }

            output.WriteLine(
                formatter.Format(entry),
                formatter.GetColor(entry.Level));
        }
    }
}