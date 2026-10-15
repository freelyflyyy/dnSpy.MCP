using System;
using dnSpy.Contracts.Extension;

namespace dnSpy.MCP.Extension {
    [ExportExtension]
    public sealed class ExtensionEntry : IExtension {
        public IEnumerable<string> MergedResourceDictionaries => Array.Empty<string>();

        public ExtensionInfo ExtensionInfo => new ExtensionInfo {
            ShortDescription = "Enable MCP integration for dnSpy"
        };
        public void OnEvent(ExtensionEvent eventType, object? data) {

        }
    }
}
