using dnSpy.MCP.Server.Doc;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace dnSpy.MCP.Server.Tools {
    [McpServerToolType]
    public sealed class ApiTools {
        private readonly ApiCatalog catalog;

        public ApiTools(ApiCatalog catalog) {
            this.catalog = catalog;
        }

        [McpServerTool(
            Name = "search_api",
            ReadOnly = true,
            Destructive = false,
            Idempotent = true,
            OpenWorld = false)]
        [Description(
            "Look up the bundled English reference for the ctx scripting API before writing code for execute_csharp. "
            + "Each entry contains Name, Signature, Description, Returns, Notes, and Example. "
            + "Performs case-insensitive substring matching across all fields; every whitespace-separated term must match, with Name matches ranked first. "
            + "An empty or whitespace-only query lists the catalog; returns [] if nothing matches. "
            + "This queries the static API documentation, not the live state of a dnSpy instance—"
            + "always replace example paths, module names, and tokens with live values from the target session.")]
        public string SearchApi(
            [Description(
                "API name or short English keywords (e.g., 'ListMethods token', 'decompile', 'ctx'). "
                + "Always translate non-English requests into English search terms first. "
                + "Omit or leave empty to browse all entries.")]
            string query = "",
            [Description("Maximum number of entries to return (1 to 20, default 5).")]
            int limit = 5) {

            try {
                return JsonSerializer.Serialize(
                    catalog.Search(query, limit));
            } catch (ArgumentException ex) {
                throw new McpException(
                    $"invalid_request: {ex.Message}");
            }
        }
    }
}
