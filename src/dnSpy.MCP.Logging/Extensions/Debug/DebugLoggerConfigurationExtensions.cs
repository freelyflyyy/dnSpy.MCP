using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging.Extensions.Debug {
    public static class DebugLoggerConfigurationExtensions{

        public static LogConfiguration Debug(
            this LogWriteToConfiguration writeTo) {

            if (writeTo is null) {
                throw new ArgumentNullException(nameof(writeTo));
            }

            return writeTo.Configure(builder => builder.AddDebug());
        }
    }
}