using dnSpy.Contracts.Output;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.ToolWindows.App;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.UI {
    [Export(typeof(McpOutput))]
    public sealed class McpOutput {
        private static readonly Guid PaneGuid = new("DEADBEEF-C0DE-4C50-0001-F2EE19C0FFEE");
        private static readonly Guid OutputWindowGuid = new("90A45E97-727E-4F31-8692-06E19218D99A");

        private readonly IOutputService outputService;
        private readonly Lazy<IDsToolWindowService> toolWindows;
        [ImportingConstructor]
        public McpOutput(IOutputService outputService, Lazy<IDsToolWindowService> toolWindows) {
            this.outputService = outputService;
            this.toolWindows = toolWindows;
        }

        public void Write(string text) {
            var pane = outputService.Create(PaneGuid, "dnSpy.MCP", "text");
            pane.WriteLine(TextColor.Text, text);
        }

        public void Show() {
            outputService.Create(PaneGuid, "dnSpy.MCP", "text");
            outputService.Select(PaneGuid);
            toolWindows.Value.Show(OutputWindowGuid);
        }

        public void WriteResult(string text) {
            Write(text);
            Show();
        }
    }
}
