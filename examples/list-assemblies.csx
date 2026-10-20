var assemblies = ctx.Documents.ListAssemblies();
var text = new System.Text.StringBuilder();

text.AppendLine($"已加载 {assemblies.Length} 个程序集：");
text.AppendLine();

foreach (var assembly in assemblies) {
    text.AppendLine(assembly.FullName);
    text.AppendLine(assembly.FilePath);
    text.AppendLine();
}

return text.ToString();