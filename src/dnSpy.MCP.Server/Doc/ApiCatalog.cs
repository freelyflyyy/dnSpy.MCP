namespace dnSpy.MCP.Server.Doc {
    public sealed class ApiCatalog {
        private readonly ApiEntry[] entries = [
            new(
                Name: "ctx",
                Signature: "DnSpyContext ctx { get;}",
                Description: "Global script entry point providing access to Documents or Decompiler",
                Returns:
                    "Documents: DocumentApi; Decompiler: DecompilerApi.",
                Notes:
                    "System and System.Linq are imported by default." +
                    "Use return to produce a JSON-serializable value." +
                    "Scripts start on the dnSpy UI thread, stay on this thread when accessing ctx." +
                    "Canceling client wait does not terminate a sumitted script.",
                Example:
                    "return ctx.Documents.ListAssemblies();"),
            new(
                Name: "ctx.Documents.ListAssemblies",
                Signature: "AssemblyInfo[] ListAssemblies()",
                Description:
                    "List loaded top-level .NET assemblies in the document tree.",
                Returns:
                    "AssemblyInfo[] with fields: FullName, FilePath.",
                Notes:
                    "Does not recursively list all dependencies. FilePath may be empty. "
                    + "Subsequent methods requiring filePath must use a non-empty path returned here.",
                Example:
                    "return ctx.Documents.ListAssemblies();"),

            new(
                Name: "ctx.Documents.ListTypes",
                Signature:
                    "TypeInfo[] ListTypes(string filePath, int limit)",
                Description:
                    "List types in a specified loaded assembly. "
                    + "Use the FilePath returned by ListAssemblies for filePath. "
                    + "limit is required and must be greater than 0.",
                Returns:
                    "TypeInfo[] with fields: ModuleName, Token, FullName, MethodCount.",
                Notes:
                    "Includes nested types and skips the special <Module> type. "
                    + "limit is the total maximum across all modules in the assembly. "
                    + "Reaching limit does not guarantee all types were listed (results may be truncated). "
                    + "Replace the example path with an actual loaded file path.",
                Example: """
                    return ctx.Documents.ListTypes(
                        @"C:\path\Example.dll",
                        100);
                    """),

            new(
                Name: "ctx.Documents.ListMethods",
                Signature:
                    "MethodInfo[] ListMethods(string filePath, string moduleName, "
                    + "uint typeToken, int limit = 100)",
                Description:
                    "List methods directly declared by a specified type. "
                    + "filePath comes from ListAssemblies. "
                    + "moduleName and typeToken come from ListTypes. "
                    + "limit must be greater than 0 (default is 100).",
                Returns:
                    "MethodInfo[] with fields: Token, FullName, HasBody.",
                Notes:
                    "moduleName is case-sensitive, and typeToken is only valid within its owning module. "
                    + "Inherited methods are not expanded. FullName includes the full signature to distinguish overloads. "
                    + "HasBody indicates whether the method has an IL body. "
                    + "Results may be truncated when the count reaches limit. "
                    + "Replace the path, module name, and token in the example with values obtained from prior queries.",
                Example: """
                    return ctx.Documents.ListMethods(
                        @"C:\path\Example.dll",
                        "Example.dll",
                        0x02000002u,
                        100);
                    """),

            new(
                Name: "ctx.Decompiler.DecompileMethod",
                Signature:
                    "string DecompileMethod(string filePath, string moduleName, "
                    + "uint methodToken)",
                Description:
                    "Decompile a specified method into C# source code. "
                    + "methodToken comes from the Token returned by ListMethods.",
                Returns:
                    "Complete decompiled C# source code string.",
                Notes:
                    "filePath must correspond to a loaded assembly. "
                    + "moduleName is case-sensitive, and methodToken is only valid within that module. "
                    + "This operation only reads and decompiles the method; it does not invoke the target method. "
                    + "Methods without an IL body may only output a declaration. "
                    + "Replace the path, module name, and token in the example with values obtained from prior queries.",
                Example: """
                    return ctx.Decompiler.DecompileMethod(
                        @"C:\path\Example.dll",
                        "Example.dll",
                        0x06000001u);
                    """)];

        public ApiEntry[] Search(string query, int limit) {
            ArgumentNullException.ThrowIfNull(query);

            if (limit < 1 || limit > 20) {
                throw new ArgumentOutOfRangeException(
                    nameof(limit), "limit must be between 1 and 20.");
            }

            var terms = query.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return entries
                .Select(entry => new {
                    Entry = entry,
                    Text = string.Join(
                        "\n",
                        entry.Name,
                        entry.Signature,
                        entry.Description,
                        entry.Returns,
                        entry.Notes,
                        entry.Example)
                })
                .Where(item => terms.All(term =>
                    item.Text.Contains(
                        term, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(item => terms.Count(term =>
                    item.Entry.Name.Contains(
                        term, StringComparison.OrdinalIgnoreCase)))
                .ThenBy(item => item.Entry.Name, StringComparer.Ordinal)
                .Take(limit)
                .Select(item => item.Entry)
                .ToArray();
        }
    }
}
