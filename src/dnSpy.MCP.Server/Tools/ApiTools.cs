using dnSpy.MCP.Logging;
using dnSpy.MCP.Server.Doc;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
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

            var watch = Stopwatch.StartNew();

            try {
                var entries = catalog.Search(query, limit);
                var json = JsonSerializer.Serialize(entries);

                watch.Stop();

                Log.i(
                    "Api.SearchCompleted",
                    $"limit={limit}; count={entries.Length}; query={query}",
                    elapsedMs: watch.ElapsedMilliseconds);

                return json;
            } catch (ArgumentException ex) {
                Log.w(
                    "Api.SearchRejected",
                    ex.Message,
                    elapsedMs: watch.ElapsedMilliseconds);

                throw new McpException($"invalid_request: {ex.Message}");
            } catch (Exception ex) {
                Log.e(
                    "Api.SearchFailed",
                    ex.ToString(),
                    elapsedMs: watch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
