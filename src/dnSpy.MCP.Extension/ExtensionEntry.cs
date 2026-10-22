using dnSpy.Contracts.Extension;
using System;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension {
    [ExportExtension]
    public sealed class ExtensionEntry : IExtension {
        private readonly ExtensionHost host;
        public IEnumerable<string> MergedResourceDictionaries => Array.Empty<string>();

        [ImportingConstructor]
        public ExtensionEntry(ExtensionHost host) {
            this.host = host;
        }
        public ExtensionInfo ExtensionInfo => new ExtensionInfo {
            ShortDescription = "Enable MCP integration for dnSpy"
        };
        public void OnEvent(ExtensionEvent eventType, object? data) {
            if (eventType == ExtensionEvent.AppExit) {
                host.RequestStop();
            }
        }
    }
}
