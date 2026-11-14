using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.Scripting;
using dnSpy.MCP.Extension.UI;
using dnSpy.MCP.Logging;
using Microsoft.Win32;
using System.ComponentModel.Composition;
using System.IO;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = McpMenuIds.Root,
        Header = "Execute C# Script",
        Icon = DsImagesAttribute.RunOutline,
        Group = McpMenuIds.ToolsGroup,
        Order = 10)]
    public sealed class RunScriptCommand : MenuItemBase {
        private readonly ScriptExecutor script;
        private readonly McpOutputView outputView;

        [ImportingConstructor]
        public RunScriptCommand(ScriptExecutor script, McpOutputView outputView) {
            this.script = script;
            this.outputView = outputView;
        }

        public override async void Execute(IMenuItemContext context) {
            try {
                var dialog = new OpenFileDialog {
                    Title = "Select C# Script to Execute",
                    Filter = "C# Scripts (*.csx)|*.csx",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() != true) {
                    return;
                }

                string code;
                using (var reader = new StreamReader(dialog.FileName)) {
                    code = await reader.ReadToEndAsync();
                }
                outputView.Show();
                await script.ExecuteAsync(code);
            } catch (Exception ex) {
                Log.E<RunScriptCommand>(
                    "Script.MenuFailed",
                    ex,
                    "Failed to execute the selected script.");
                outputView.Show();
            }
        }
    }
}
