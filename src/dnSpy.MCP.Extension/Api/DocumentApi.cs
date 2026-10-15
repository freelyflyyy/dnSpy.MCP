using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Api {
    [Export(typeof(DocumentApi))]
    public sealed class DocumentApi {
        private readonly IDsDocumentService documentService;

        [ImportingConstructor]
        public DocumentApi(IDsDocumentService documentService) {
            this.documentService = documentService;
        }

        public AssemblyDef GetAssembly(string filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }

            var matches = documentService.GetDocuments()
                .Where(document =>
                    document.AssemblyDef is not null &&
                    string.Equals(document.Filename, filePath, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length != 1) {
                throw new InvalidOperationException($"Expected to match exactly one loaded assembly, but found {matches.Length}.");
            }

            return matches[0].AssemblyDef!;
        }

        public AssemblyInfo[] ListAssemblies() {
            var assemblies = new List<AssemblyInfo>();

            foreach (var document in documentService.GetDocuments()) {
                var assembly = document.AssemblyDef;

                if (assembly is null) {
                    continue;
                }

                assemblies.Add(new AssemblyInfo(
                    assembly.FullName,
                    document.Filename ?? string.Empty));
            }

            return assemblies.ToArray();
        }

        public TypeInfo[] ListTypes(string filePath, int limit) {
            if (limit <= 0) {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            var assembly = GetAssembly(filePath);
            var types = new List<TypeInfo>();

            foreach (var module in assembly.Modules) {
                foreach (var type in module.GetTypes()) {
                    if (type.IsGlobalModuleType) {
                        continue;
                    }

                    types.Add(new TypeInfo(
                        module.Name.String,
                        type.MDToken.Raw,
                        type.FullName,
                        type.Methods.Count));

                    if (types.Count >= limit) {
                        return types.ToArray();
                    }
                }
            }

            return types.ToArray();
        }
        public MethodInfo[] ListMethods(string filePath,string moduleName, uint typeToken, int limit = 100) {
            if (string.IsNullOrWhiteSpace(moduleName)) {
                throw new ArgumentException("Module name is cannot be null or whitespace.", nameof(moduleName));
            }

            if (limit <= 0) {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            var assembly = GetAssembly(filePath);
            var modules = assembly.Modules
                .Where(module => string.Equals(
                    module.Name.String,
                    moduleName,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (modules.Length != 1) {
                throw new InvalidOperationException($"Expected to match exactly one module, but found {modules.Length}.");
            }

            var module = modules[0];

            if (module.ResolveToken(typeToken) is not TypeDef type) {
                throw new ArgumentException(
                    $"Token 0x{typeToken:X8} does not identify a type.",
                    nameof(typeToken));
            }

            return type.Methods
                .Take(limit)
                .Select(method => new MethodInfo(
                    method.MDToken.Raw,
                    method.FullName,
                    method.HasBody))
                .ToArray();
        }
    }
}
