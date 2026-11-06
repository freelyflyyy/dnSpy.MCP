using System.Diagnostics;

namespace dnSpy.MCP.Logging {
    public sealed class Logger {
        private readonly ILogSink sink;
        private readonly int previewLength;
        public Logger(ILogSink sink, int previewLength = 512) {
            this.sink = sink
                ?? throw new ArgumentNullException(nameof(sink));
            if (previewLength <= 0) {
                throw new ArgumentOutOfRangeException(nameof(previewLength));
            }
            this.previewLength = previewLength;
        }

        public void Write(
            LogLevel level,
            string eventName,
            string message,
            string? result = null,
            long? elapsedMs = null) {

            try {
                var time = DateTimeOffset.Now;
                var text = Clip(message ?? "", out var messageCut);

                var resultCut = false;
                var preview = result is null
                    ? null
                    : Clip(result, out resultCut);

                var entry = new LogEntry(
                    time,
                    level,
                    eventName,
                    text,
                    preview,
                    elapsedMs,
                    messageCut,
                    resultCut);

                sink.Write(entry);
            } catch (Exception ex) {
                try {
                    Trace.WriteLine($"Logger failed: {ex}");
                } catch {
                }
            }
        }

        private string Clip(string text, out bool truncated) {
            int length = Math.Min(text.Length, previewLength);

            if (length < text.Length && length > 0
                && char.IsHighSurrogate(text[length - 1])
                && char.IsLowSurrogate(text[length])) {
                length--;
            }

            truncated = length < text.Length;
            return truncated ? text.Substring(0, length) : text;
        }
    }
}