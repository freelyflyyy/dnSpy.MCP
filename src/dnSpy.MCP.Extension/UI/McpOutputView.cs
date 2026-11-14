using dnSpy.Contracts.Output;
using dnSpy.Contracts.ToolWindows.App;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.UI {
    [Export(typeof(McpOutputView))]
    public sealed class McpOutputView {
        private static readonly Guid WindowGuid = new("90A45E97-727E-4F31-8692-06E19218D99A");

        private readonly IOutputService outputService;
        private readonly Lazy<IDsToolWindowService> toolWindows;

        [ImportingConstructor]
        public McpOutputView(IOutputService outputService, Lazy<IDsToolWindowService> toolWindows) {
            this.outputService = outputService;
            this.toolWindows = toolWindows;
        }

        public void Show() {
            outputService.Create(
                McpOutput.PaneGuid,
                "dnSpy.MCP",
                "text");

            outputService.Select(McpOutput.PaneGuid);
            toolWindows.Value.Show(WindowGuid);
        }
    }
}
