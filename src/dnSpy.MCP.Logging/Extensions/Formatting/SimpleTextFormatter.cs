using System.Globalization;
using System.Text;

namespace dnSpy.MCP.Logging.Extensions.Formatting {
    public sealed class SimpleTextFormatter : ILogEntryFormatter {
        public string Format(LogEntry entry) {
            if (entry is null) {
                throw new ArgumentNullException(nameof(entry));
            }

            var text = new StringBuilder()
                .Append('[')
                .Append(entry.Timestamp.ToString(
                    "yyyy-MM-dd HH:mm:ss.fff",
                    CultureInfo.InvariantCulture))
                .Append("] [").Append(entry.Level)
                .Append("] [").Append(Escape(entry.Category))
                .Append(']');

            if (entry.EventId.Id != 0 || entry.EventId.Name is not null) {
                text.Append(" [event=")
                    .Append(entry.EventId.Id.ToString(
                        CultureInfo.InvariantCulture));

                if (entry.EventId.Name is not null) {
                    text.Append(':')
                        .Append(Escape(entry.EventId.Name));
                }

                text.Append(']');
            }

            text.Append(' ')
                .Append(Escape(entry.FormattedMessage));

            foreach (var property in entry.Properties) {
                text.Append(" | ")
                    .Append(Escape(property.Key))
                    .Append('=')
                    .Append(FormatValue(property.Value));
            }

            foreach (var scope in entry.Scopes) {
                text.Append(" | scope=")
                    .Append(FormatScope(scope));
            }

            if (entry.Exception is not null) {
                text.Append(" | exception=")
                    .Append(Escape(entry.Exception.ToString()));
            }

            return text.ToString();
        }

        private static string FormatScope(object? scope) {
            if (scope is IEnumerable<KeyValuePair<string, object?>> properties) {
                return "{" + string.Join(
                    ", ",
                    properties.Select(property =>
                        Escape(property.Key) + "=" +
                        FormatValue(property.Value))) + "}";
            }

            return FormatValue(scope);
        }

        private static string FormatValue(object? value) {
            if (value is null) {
                return "null";
            }

            var text = value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString();

            return Escape(text ?? "");
        }

        private static string Escape(string text) =>
            text.Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("|", "\\|")
                .Replace("]", "\\]");
    }
}