using dnSpy.MCP.Logging.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging {
    public sealed class LogWriteToConfiguration {
        private readonly LogConfiguration configuration;

        internal LogWriteToConfiguration(LogConfiguration configuration) {
            this.configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public LogConfiguration Configure(Action<ILoggingBuilder> configure) {
            configuration.AddConfiguration(configure);
            return configuration;
        }

        public LogConfiguration Sink(ILogSink sink) {
            if (sink is null) {
                throw new ArgumentNullException(nameof(sink));
            }

            return Sink(_ => sink, ownsSink: false);
        }

        public LogConfiguration Sink(
            Func<IServiceProvider, ILogSink> factory,
            bool ownsSink) {
            if (factory is null) {
                throw new ArgumentNullException(nameof(factory));
            }

            return Configure(builder => {
                builder.Services.AddSingleton<ILoggerProvider>(services => {
                    var sink = factory(services)
                        ?? throw new InvalidOperationException(
                            "The sink factory returned null.");

                    return new SinkLoggerProvider(sink, ownsSink);
                });
            });
        }

        public LogConfiguration Provider(ILoggerProvider provider) {
            if (provider is null) {
                throw new ArgumentNullException(nameof(provider));
            }

            return Configure(builder => builder.AddProvider(provider));
        }

        public LogConfiguration Provider(
            Func<IServiceProvider, ILoggerProvider> factory) {

            if (factory is null) {
                throw new ArgumentNullException(nameof(factory));
            }

            return Configure(builder => {
                builder.Services.AddSingleton<ILoggerProvider>(services =>
                    factory(services)
                        ?? throw new InvalidOperationException(
                            "The provider factory returned null."));
            });
        }
    }
}
