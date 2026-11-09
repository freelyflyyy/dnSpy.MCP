using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging.Providers {
    internal sealed class SinkLogger : ILogger {
        private readonly string category;
        private readonly SinkLoggerProvider provider;

        public SinkLogger(string category, SinkLoggerProvider provider) {
            this.category = category;
            this.provider = provider;
        }

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Trace &&
            logLevel < LogLevel.None &&
            !provider.IsDisposed;


        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            provider.IsDisposed
                ? null
                : provider.ScopeProvider.Push(state);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (!IsEnabled(logLevel)) {
                return;
            }

            if (formatter is null) {
                throw new ArgumentNullException(nameof(formatter));
            }

            var timestamp = DateTimeOffset.Now;
            string? messageTemplate = null;

            var properties = new List<KeyValuePair<string, object?>>();

            if (state is IEnumerable<KeyValuePair<string, object?>>
                structuredState) {

                foreach (var property in structuredState) {
                    if (property.Key == "{OriginalFormat}" &&
                        property.Value is string template) {

                        messageTemplate = template;
                    } else {
                        properties.Add(property);
                    }
                }
            }

            var scopes = new List<object?>();

            provider.ScopeProvider.ForEachScope((scope, item) => {
                if (scope is IEnumerable<KeyValuePair<string, object?>>
                    scopeProperties) {

                    item.Add(Array.AsReadOnly(scopeProperties.ToArray()));
                } else {
                    item.Add(scope);
                }
            }, scopes);

            provider.Write(new LogEntry(
                Timestamp: timestamp,
                Level: logLevel,
                Category: category,
                EventId: eventId,
                MessageTemplate: messageTemplate,
                FormattedMessage: formatter(state, exception),
                Exception: exception,
                Properties: properties,
                Scopes: scopes));
        }
    }
}