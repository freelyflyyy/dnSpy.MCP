using System.ComponentModel.Composition;

namespace dnSpy.MCP.Extension.Api {
    [Export(typeof(DnSpyContext))]
    public sealed class DnSpyContext {
        public DocumentApi Documents { get; }
        public DecompilerApi Decompiler { get; }

        [ImportingConstructor]
        public DnSpyContext(DocumentApi documents, DecompilerApi decompiler) {
            Documents = documents;
            Decompiler = decompiler;
        }
    }
}
