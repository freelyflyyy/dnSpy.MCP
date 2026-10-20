using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.Scripting;
using dnSpy.MCP.Extension.UI;
using Microsoft.Win32;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.Json;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID,
        Header = "运行 C# 脚本",
        Icon = "icon_mcp",
        Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS,
        Order = 1000
        )]
    public sealed class ServerMenus : MenuItemBase {
        private readonly ScriptExecutor script;
        private readonly McpOutput output;

        [ImportingConstructor]
        public ServerMenus(ScriptExecutor script, McpOutput output) {
            this.script = script;
            this.output = output;
        }

        public override async void Execute(IMenuItemContext context) {
            try {
                var dialog = new OpenFileDialog {
                    Title = "选择要运行的 C# 脚本",
                    Filter = "C# 脚本 (*.csx)|*.csx",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() != true) {
                    return;
                }

                var uiThreadId = Environment.CurrentManagedThreadId;
                var code = await File.ReadAllTextAsync(dialog.FileName);
                var result = await Task.Run(() => script.ExecuteAsync(code));

                var json = JsonSerializer.Serialize(
                    result,
                    new JsonSerializerOptions {
                        WriteIndented = true
                    });

                output.WriteResult(
                    $"UI 线程 ID：{uiThreadId}"
                    + Environment.NewLine
                    + json);
            } catch (Exception ex) {
                output.WriteResult(
                    $"读取脚本文件或显示结果失败：{ex.GetType().Name}"
                    + Environment.NewLine
                    + ex.Message);
            }
        }
    }
}
