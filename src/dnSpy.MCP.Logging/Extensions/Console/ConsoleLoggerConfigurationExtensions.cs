using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace dnSpy.MCP.Logging.Extensions.Console {
    public static class ConsoleLoggerConfigurationExtensions {
        public static LogConfiguration Console(
            this LogWriteToConfiguration writeTo,
            Action<ConsoleLoggerOptions>? configure = null) {

            if (writeTo is null) {
                throw new ArgumentNullException(nameof(writeTo));
            }

            return writeTo.Configure(builder => builder.AddConsole(options => {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
                configure?.Invoke(options);
            }));
        }
    }
}