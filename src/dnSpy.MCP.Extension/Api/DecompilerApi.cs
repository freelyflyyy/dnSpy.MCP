using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.MCP.Extension.Api {
    [Export(typeof(DecompilerApi))]
    public sealed class DecompilerApi {
        private readonly DocumentApi documentApi;
        private readonly IDecompilerService decompilerService;

        [ImportingConstructor]
        public DecompilerApi(DocumentApi documentApi, IDecompilerService decompilerService) {
            this.documentApi = documentApi;
            this.decompilerService = decompilerService;
        }

        public string DecompileMethod(string filePath, string moduleName, uint methodToken) {
            var module = documentApi.GetModule(filePath, moduleName);

            if (module.ResolveToken(methodToken) is not MethodDef method) {
                throw new ArgumentException($"Token 0x{methodToken:X8} does not identify a method.", nameof(methodToken));
            }

            var decomplier = decompilerService.Find(DecompilerConstants.LANGUAGE_CSHARP);
            if (decomplier == null) {
                throw new InvalidOperationException("The C# decomplier is unavailable");
            }

            var output = new StringBuilderDecompilerOutput();
            var context = new DecompilationContext { AsyncMethodBodyDecompilation = false };

            decomplier.Decompile(method, output, context);

            return output.GetText();
        }

    }
}
