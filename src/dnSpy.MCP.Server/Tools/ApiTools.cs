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
            "Search the bundled English reference for ctx API signatures and examples. "
            + "This searches documentation, not live assemblies. "
            + "All whitespace-separated terms must match, case-insensitively. "
            + "Use an API name or a short keyword; omit the query to browse up to limit entries. "
            + "Get actual file paths, module names, and metadata tokens from the target host.")]
        public string SearchApi(
            [Description(
                "An API name or short English search terms, e.g. ListMethods or token. "
                + "Omit or pass an empty string to browse.")]
            string query = "",
            [Description("Maximum entries to return (1 to 20, default 5). Set to 20 when browsing with an empty query.")]
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
