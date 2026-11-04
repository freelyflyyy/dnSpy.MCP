using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.UI;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = McpMenuIds.Root,
        Header = "Service Status",
        Group = McpMenuIds.ServiceGroup,
        Order = 0)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ServiceStatusCommand : MenuItemBase {
        private readonly ExtensionHost host;

        [ImportingConstructor]
        public ServiceStatusCommand(ExtensionHost host) => this.host = host;
        public override string? GetHeader(IMenuItemContext context) =>
            host.State switch {
                ServiceState.Stopped => "MCP Status: Stopped",
                ServiceState.Starting => "MCP Status: Starting...",
                ServiceState.Running => "MCP Status: Running",
                ServiceState.Stopping => "MCP Status: Stopping...",
                ServiceState.Faulted => "MCP Status: Faulted, Please Check Logs",
                _ => "MCP Status: Unknown State"
            };
        public override bool IsEnabled(IMenuItemContext context) => false;
        public async override void Execute(IMenuItemContext context) {
        }
    }

    [ExportMenuItem(
        OwnerGuid = McpMenuIds.Root,
        Header = "Start Service",
        Group = McpMenuIds.ServiceGroup,
        Order = 10)]
    internal sealed class StartServiceCommand : MenuItemBase {
        private readonly ExtensionHost host;
        [ImportingConstructor]
        public StartServiceCommand(ExtensionHost host) => this.host = host;
        public override bool IsEnabled(IMenuItemContext context) => host.CanStart;
        public override void Execute(IMenuItemContext context) => host.Start();
    }

    [ExportMenuItem(
        OwnerGuid = McpMenuIds.Root,
        Header = "Stop Service",
        Group = McpMenuIds.ServiceGroup,
        Order = 20)]
    internal sealed class StopServiceCommand : MenuItemBase {
        private readonly ExtensionHost host;
        [ImportingConstructor]
        public StopServiceCommand(ExtensionHost host) => this.host = host;
        public override bool IsEnabled(IMenuItemContext context) => host.CanStop;
        public override void Execute(IMenuItemContext context) => host.RequestStop();
    }

    [ExportMenuItem(
        OwnerGuid = McpMenuIds.Root,
        Header = "View Logs",
        Group = McpMenuIds.ServiceGroup,
        Order = 0)]
    internal sealed class ViewLogsCommand : MenuItemBase {
        private readonly McpOutput output;
        [ImportingConstructor]
        public ViewLogsCommand(McpOutput output) => this.output = output;
        public override void Execute(IMenuItemContext context) => output.Show();
    }
}
