using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.Api;
using dnSpy.MCP.Extension.UI;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID,
        Header = "反编译到输出窗口",
        Icon = "icon_mcp",
        Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS,
        Order = 1000
        )]
    public sealed class ServerMenus : MenuItemBase {
        private readonly DocumentApi documents;
        private readonly DecompilerApi decompiler;
        private readonly McpOutput output;

        [ImportingConstructor]
        public ServerMenus(DocumentApi documents, DecompilerApi decompiler, McpOutput output) {
            this.documents = documents;
            this.decompiler = decompiler;
            this.output = output;
        }

        public override void Execute(IMenuItemContext context) {
            var assemblies = documents.ListAssemblies()
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.FilePath));

            if (assemblies is null) {
                MsgBox.Instance.Show("没有具有文件路径的已加载的程序集");
                return;
            }

            try {
                const int typeLimit = 100;
                const int methodLimit = 30;

                var type = documents.ListTypes(assemblies.FilePath, typeLimit)
                    .FirstOrDefault(item => item.MethodCount > 0);

                if (type is null) {
                    MsgBox.Instance.Show(
                        $"在前 {typeLimit} 个类型的查询范围内，没有找到包含方法的类型。");
                    return;
                }

                var methods = documents.ListMethods(
                    assemblies.FilePath,
                    type.ModuleName,
                    type.Token,
                    methodLimit);

                var method = methods.FirstOrDefault(item => item.HasBody);

                if (method is null) {
                    MsgBox.Instance.Show("当前获取到的函数都没有方法体");
                    return;
                }

                var code = decompiler.DecompileMethod(
                    assemblies.FilePath,
                    type.ModuleName,
                    method.Token);

                output.WriteResult(
                    $"程序集：{assemblies.FullName}"
                    + Environment.NewLine
                    + $"路径：{assemblies.FilePath}"
                    + Environment.NewLine
                    + $"模块：{type.ModuleName}"
                    + Environment.NewLine
                    + $"方法：0x{method.Token:X8}  {method.FullName}"
                    + Environment.NewLine
                    + Environment.NewLine
                    + code);
            } catch (ArgumentException ex) {
                MsgBox.Instance.Show($"参数错误：{ex.Message}");
            } catch (InvalidOperationException ex) {
                MsgBox.Instance.Show($"操作错误：{ex.Message}");
            }

        }
    }
}
