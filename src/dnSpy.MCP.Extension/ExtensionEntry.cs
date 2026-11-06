using dnSpy.Contracts.Extension;
using dnSpy.MCP.Logging;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension {
    [ExportExtension]
    public sealed class ExtensionEntry : IExtension {
        private readonly ExtensionHost host;
        private readonly ILogSink logSink;
        public IEnumerable<string> MergedResourceDictionaries => Array.Empty<string>();

        [ImportingConstructor]
        public ExtensionEntry(ExtensionHost host, ILogSink logSink) {
            this.host = host;
            this.logSink = logSink;
        }
        public ExtensionInfo ExtensionInfo => new ExtensionInfo {
            ShortDescription = "Enable MCP integration for dnSpy"
        };
        public void OnEvent(ExtensionEvent eventType, object? data) {
            if (eventType == ExtensionEvent.AppLoaded) {
                Log.Initialize(logSink);
                host.Start();
            } else if (eventType == ExtensionEvent.AppExit) {
                host.RequestStop();
            }
        }
    }
}
