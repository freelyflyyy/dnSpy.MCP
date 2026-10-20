using dnSpy.MCP.Extension.Api;

namespace dnSpy.MCP.Extension.Scripting {
    public sealed class ScriptGlobals {
        public DnSpyContext ctx { get; }

        public ScriptGlobals(DnSpyContext context) {
            ctx = context;
        }
    }
}
