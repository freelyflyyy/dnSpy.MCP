using System.Globalization;
using System.Text;

namespace dnSpy.MCP.Logging {
    public static class TextLogFormatter {
        public static string Format(LogEntry entry) {
            var time = entry.Timestamp.ToString(
                "HH:mm:ss.fff",
                CultureInfo.InvariantCulture);

            var text = new StringBuilder()
                .Append('[').Append(time)
                .Append("] [").Append(entry.Level)
                .Append("] [").Append(Escape(entry.EventName))
                .Append("] ").Append(Escape(entry.Message));

            if (entry.MessageTruncated) {
                text.Append("… [truncated]");
            }

            if (entry.ElapsedMs.HasValue) {
                text.Append(" | elapsed=")
                    .Append(entry.ElapsedMs.Value.ToString(
                        CultureInfo.InvariantCulture))
                    .Append("ms");
            }

            if (entry.ResultPreview is not null) {
                text.Append(" | result=")
                    .Append(Escape(entry.ResultPreview));

                if (entry.ResultTruncated) {
                    text.Append("… [truncated]");
                }
            }

            return text.ToString();
        }

        private static string Escape(string text) =>
            text.Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
    }
}