using dnSpy.MCP.Logging.Extensions.Formatting;

namespace dnSpy.MCP.Logging.Extensions.Trace {
    public static class TraceLoggerConfigurationExtensions {

        public static LogConfiguration Trace(
            this LogWriteToConfiguration writeTo,
            ILogEntryFormatter? formatter = null) {

            if (writeTo is null) {
                throw new ArgumentNullException(nameof(writeTo));
            }

            return writeTo.Sink(
                _ => new TraceLogSink(formatter),
                ownsSink: true);
        }
    }
}
