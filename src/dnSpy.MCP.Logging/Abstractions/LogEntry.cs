using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace dnSpy.MCP.Logging {
    public sealed class LogEntry {
        public DateTimeOffset Timestamp { get; }
        public LogLevel Level { get; }
        public string Category { get; }
        public EventId EventId { get; }
        public string? MessageTemplate { get; }
        public string FormattedMessage { get; }
        public Exception? Exception { get; }

        public IReadOnlyDictionary<string, object?> Properties { get; }
        public IReadOnlyList<object?> Scopes { get; }

        public LogEntry(
            DateTimeOffset Timestamp,
            LogLevel Level,
            string Category,
            EventId EventId,
            string? MessageTemplate,
            string FormattedMessage,
            Exception? Exception = null,
            IEnumerable<KeyValuePair<string, object?>>? Properties = null,
            IEnumerable<object?>? Scopes = null) {

            this.Timestamp = Timestamp;
            this.Level = Level;
            this.Category = Category
                ?? throw new ArgumentNullException(nameof(Category));
            this.EventId = EventId;
            this.MessageTemplate = MessageTemplate;
            this.FormattedMessage = FormattedMessage
                ?? throw new ArgumentNullException(nameof(FormattedMessage));
            this.Exception = Exception;
            var propertySnapshot = new Dictionary<string, object?>(StringComparer.Ordinal);

            if (Properties is not null) {
                foreach (var property in Properties) {
                    propertySnapshot[property.Key] = property.Value;
                }
            }

            this.Properties = new ReadOnlyDictionary<string, object?>(propertySnapshot);
            this.Scopes = Array.AsReadOnly(Scopes?.ToArray() ?? Array.Empty<object?>());
        }
    }
}
