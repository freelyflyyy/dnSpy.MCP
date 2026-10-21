using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.Transport;
using dnSpy.MCP.Extension.UI;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID,
        Header = "接收一次管道请求",
        Icon = "icon_mcp",
        Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS,
        Order = 1010)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class PipeTestMenu : MenuItemBase {
        private readonly PipeServer server;
        private readonly McpOutput output;
        private bool running;
        [ImportingConstructor]
        public PipeTestMenu(PipeServer server, McpOutput output) {
            this.server = server;
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
                output.WriteResult(
                    $"管道名：{server.PipeName}"
                    + Environment.NewLine
                    + "等待一次请求，接收超时为 60 秒。");

                await Task.Run(() => server.ServeOnceAsync());
                output.WriteResult(
                    $"本次管道处理已结束：{server.PipeName}");
            } catch (OperationCanceledException) {
                output.WriteResult("管道接收或发送超时。");
            } catch (Exception ex) {
                output.WriteResult(
                    $"管道失败：{ex.GetType().Name}"
                    + Environment.NewLine
                    + ex.Message);
            } finally {
                running = false;
            }
        }
    }
}
