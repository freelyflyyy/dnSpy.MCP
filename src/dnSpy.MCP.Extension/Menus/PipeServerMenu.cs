using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.Transport;
using dnSpy.MCP.Extension.UI;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID,
        Header = "打开/关闭管道服务",
        Icon = "icon_mcp",
        Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS,
        Order = 1010)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class PipeServerMenu : MenuItemBase {
        private readonly ExtensionHost host;
        private readonly McpOutput output;
        private bool running;

        [ImportingConstructor]
        public PipeServerMenu(ExtensionHost host, McpOutput output) {
            this.host = host;
            this.output = output;
        }
        public override bool IsEnabled(IMenuItemContext context) {
            return !running;
        }
        public async override void Execute(IMenuItemContext context) {
            if (running) {
                return;
            }
            running = true;
            try {
                if (host.IsRunning) {
                    output.WriteResult(
                        "Service is being stopped; " +
                        Environment.NewLine +
                        "accepted scripts will complete and attempt to return results.");

                    await host.StopAsync();
                } else {
                    output.WriteResult(
                        $"Start the pipeline service:{host.PipeName}");

                    host.Start();
                }
            } catch (Exception ex) {
                output.WriteResult(
                    $"Service operation failed:{ex.GetType().Name}"
                    + Environment.NewLine
                    + ex.Message);
            } finally {
                running = false;
            }
        }
    }
}
