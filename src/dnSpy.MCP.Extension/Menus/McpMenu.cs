using dnSpy.Contracts.Menus;

namespace dnSpy.MCP.Extension.Menus {
    internal sealed class McpMenuIds {
        public const string Root = "AC6F18ED-52C7-436A-A2E5-C0FC4C9F7AC8";
        public const string ServiceGroup = "0,51C6A1D9-7602-46DD-90C5-381A01F9E28B";
        public const string ToolsGroup = "10, 1A2B3C4D-5E6F-7A8B-9C0D-1E2F3A4B5C6D";
    }

    [ExportMenu(
        OwnerGuid = MenuConstants.APP_MENU_GUID,
        Guid = McpMenuIds.Root,
        Header = "MCP",
        Order = MenuConstants.ORDER_APP_MENU_DEBUG + 0.1)]
    internal sealed class McpMenu : IMenu {
    }
}
