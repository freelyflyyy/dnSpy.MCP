using dnSpy.Contracts.Output;
using dnSpy.Contracts.Text;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.UI {
    [Export(typeof(McpOutput))]
    public sealed class McpOutput {
        private static readonly Guid PaneGuid = new("DEADBEEF-C0DE-4C50-0001-F2EE19C0FFEE");
        private readonly IOutputService outputService;
        [ImportingConstructor]
        public McpOutput(IOutputService outputService) {
            this.outputService = outputService;
        }

        public void WriteResult(string text) {
            var pane = outputService.Create(PaneGuid, "dnSpy.MCP", "text");
            pane.WriteLine(TextColor.Text, text);

            outputService.Select(PaneGuid);
        }
    }
}
