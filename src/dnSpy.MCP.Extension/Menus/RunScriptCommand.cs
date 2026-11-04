using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.Scripting;
using dnSpy.MCP.Extension.UI;
using Microsoft.Win32;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.Json;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = McpMenuIds.Root,
        Header = "Execute C# Script",
        Icon = "icon_mcp",
        Group = McpMenuIds.ToolsGroup,
        Order = 10)]
    public sealed class RunScriptCommand : MenuItemBase {
        private readonly ScriptExecutor script;
        private readonly McpOutput output;

        [ImportingConstructor]
        public RunScriptCommand(ScriptExecutor script, McpOutput output) {
            this.script = script;
            this.output = output;
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
                var result = await Task.Run(() => script.ExecuteAsync(code));

                var json = JsonSerializer.Serialize(
                    result,
                    new JsonSerializerOptions {
                        WriteIndented = true
                    });

                output.WriteResult(json);
            } catch (Exception ex) {
                output.WriteResult(
                    $"Failed to read script file or display result: {ex.GetType().Name}"
                    + Environment.NewLine
                    + ex.Message);
            }
        }
    }
}
