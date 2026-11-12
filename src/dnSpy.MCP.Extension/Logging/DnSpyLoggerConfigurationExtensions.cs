using dnSpy.MCP.Logging;

namespace dnSpy.MCP.Extension.Logging {
    internal static class DnSpyLoggerConfigurationExtensions {

        public static LogConfiguration DnSpy(
            this LogWriteToConfiguration writeTo,
            ILogSink logSink) {
            if (writeTo is null) {
                throw new ArgumentNullException(nameof(writeTo));
            }

            if (logSink is null) {
                throw new ArgumentNullException(nameof(logSink));
            }

            return writeTo.Sink(logSink);
        }
    }
}
