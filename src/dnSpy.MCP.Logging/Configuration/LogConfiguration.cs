using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging {
    public sealed class LogConfiguration {
        private readonly List<Action<ILoggingBuilder>> configureActions =
            new List<Action<ILoggingBuilder>>();

        public LogMinimumLevelConfiguration MinimumLevel { get; }
        public LogWriteToConfiguration WriteTo { get; }

        public LogConfiguration() {
            MinimumLevel = new LogMinimumLevelConfiguration(this);
            WriteTo = new LogWriteToConfiguration(this);
        }

        internal void AddConfiguration(Action<ILoggingBuilder> configure) {
            if (configure is null) {
                throw new ArgumentNullException(nameof(configure));
            }

            configureActions.Add(configure);
        }

        public void ApplyTo(ILoggingBuilder builder) {
            if (builder is null) {
                throw new ArgumentNullException(nameof(builder));
            }

            var actions = configureActions.ToArray();

            foreach (var configure in actions) {
                configure(builder);
            }
        }

        public LoggingRuntime CreateRuntime() {
            var services = new ServiceCollection();
            services.AddLogging(ApplyTo);

            var serviceProvider = services.BuildServiceProvider();

            try {
                var factory = serviceProvider.GetRequiredService<ILoggerFactory>();

                return new LoggingRuntime(factory, serviceProvider);
            } catch (Exception creationError) {
                try {
                    serviceProvider.Dispose();
                } catch (Exception cleanupError) {
                    throw new AggregateException(
                        "Failed to create the logging runtime and release its resources.",
                        creationError,
                        cleanupError);
                }
                throw;
            }
        }
    }
}
