using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.MCP.Extension.Api;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Menus {
    [ExportMenuItem(
        OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID,
        Header = "查看已加载文档",
        Icon = "icon_mcp",
        Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS,
        Order = 1000
        )]
    public sealed class ServerMenus : MenuItemBase {
        private readonly DocumentApi documents;

        [ImportingConstructor]
        public ServerMenus(DocumentApi documents) {
            this.documents = documents;
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

                var entries = methods.Select(method =>
                    $"0x{method.Token:X8}  {method.FullName}"
                    + Environment.NewLine
                    + $"包含 IL 方法体：{method.HasBody}");

                MsgBox.Instance.Show(
                    $"程序集：{assemblies.FullName}"
                    + Environment.NewLine
                    + $"路径：{assemblies.FilePath}"
                    + Environment.NewLine
                    + $"模块：{type.ModuleName}"
                    + Environment.NewLine
                    + $"类型：0x{type.Token:X8}  {type.FullName}"
                    + Environment.NewLine
                    + $"显示 {methods.Length} 个方法，最多 {methodLimit} 个："
                    + Environment.NewLine
                    + Environment.NewLine
                    + string.Join(
                        Environment.NewLine + Environment.NewLine,
                        entries));
            } catch (ArgumentException ex) {
                MsgBox.Instance.Show($"参数错误：{ex.Message}");
            } catch (InvalidOperationException ex) {
                MsgBox.Instance.Show($"操作错误：{ex.Message}");
            }

        }
    }
}
