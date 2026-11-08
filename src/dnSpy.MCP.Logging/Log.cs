using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging {
    public static class Log {

        private static Logger? current;

        public static void Initialize(
            ILogSink sink,
            int previewLength = 512) {
            var logger = new Logger(sink, previewLength);
            if (Interlocked.CompareExchange(ref current, logger, null) is not null) {
                throw new InvalidOperationException(
                    "Logging is already initialized.");
            }
        }
        public static void i(
            string eventName,
            string message,
            string? result = null,
            long? elapsedMs = null) {

            Volatile.Read(ref current)?.Write(
                LogLevel.Information,
                eventName,
                message,
                result,
                elapsedMs);
        }

        public static void w(
            string eventName,
            string message,
            string? result = null,
            long? elapsedMs = null) {
            Volatile.Read(ref current)?.Write(
                LogLevel.Warning,
                eventName,
                message,
                result,
                elapsedMs);
        }

        public static void e(
            string eventName,
            string message,
            string? result = null,
            long? elapsedMs = null) {
            Volatile.Read(ref current)?.Write(
                LogLevel.Error,
                eventName,
                message,
                result,
                elapsedMs);
        }
    }
}
