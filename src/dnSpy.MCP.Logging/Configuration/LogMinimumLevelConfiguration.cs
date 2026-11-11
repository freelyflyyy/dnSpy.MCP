using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging {
    public sealed class LogMinimumLevelConfiguration {
        private readonly LogConfiguration configuration;

        internal LogMinimumLevelConfiguration(
            LogConfiguration configuration) {
            this.configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public LogConfiguration Default(LogLevel level) {
            ValidateLevel(level);

            configuration.AddConfiguration(
                builder => builder.SetMinimumLevel(level));

            return configuration;
        }

        public LogConfiguration Override(string category, LogLevel level) {
            if (category is null) {
                throw new ArgumentNullException(nameof(category));
            }

            if (string.IsNullOrWhiteSpace(category)) {
                throw new ArgumentException(
                    "A category prefix is required.",
                    nameof(category));
            }

            ValidateLevel(level);

            configuration.AddConfiguration(
                builder => builder.AddFilter(category, level));

            return configuration;
        }

        public LogConfiguration Trace() => Default(LogLevel.Trace);
        public LogConfiguration Debug() => Default(LogLevel.Debug);
        public LogConfiguration Information() => Default(LogLevel.Information);
        public LogConfiguration Warning() => Default(LogLevel.Warning);
        public LogConfiguration Error() => Default(LogLevel.Error);
        public LogConfiguration Critical() => Default(LogLevel.Critical);
        public LogConfiguration None() => Default(LogLevel.None);

        private static void ValidateLevel(LogLevel level) {
            if (level < LogLevel.Trace || level > LogLevel.None) {
                throw new ArgumentOutOfRangeException(nameof(level));
            }
        }
    }
}
