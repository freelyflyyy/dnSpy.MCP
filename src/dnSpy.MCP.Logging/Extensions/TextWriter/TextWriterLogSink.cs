using dnSpy.MCP.Logging.Extensions.Formatting;

namespace dnSpy.MCP.Logging.Extensions.TextWriter {

    public sealed class TextWriterLogSink : ILogSink, IDisposable {
        private readonly System.IO.TextWriter writer;
        private readonly ILogEntryFormatter formatter;
        private readonly bool leaveOpen;
        private readonly bool autoFlush;
        private readonly object gate = new object();

        private bool disposed;

        public TextWriterLogSink(
            System.IO.TextWriter writer,
            bool leaveOpen = true,
            TextWriterSinkOptions? options = null) {

            this.writer = writer
                ?? throw new ArgumentNullException(nameof(writer));

            var setting = options ?? new TextWriterSinkOptions();

            formatter = setting.Formatter
                ?? throw new ArgumentException(
                    "A formatter is required.",
                    nameof(options));

            this.leaveOpen = leaveOpen;
            autoFlush = setting.AutoFlush;
        }

        public void Write(LogEntry entry) {
            if (entry is null) {
                throw new ArgumentNullException(nameof(entry));
            }

            lock (gate) {
                if (disposed) {
                    throw new ObjectDisposedException(
                        nameof(TextWriterLogSink));
                }

                writer.WriteLine(formatter.Format(entry));

                if (autoFlush) {
                    writer.Flush();
                }
            }
        }

        public void Dispose() {
            lock (gate) {
                if (disposed) {
                    return;
                }

                disposed = true;

                if (leaveOpen) {
                    writer.Flush();
                    return;
                }

                try {
                    writer.Flush();
                } catch(Exception flushError) {
                    try {
                        writer.Dispose();
                    }catch(Exception disposeError) {
                        throw new AggregateException(
                            "Failed to flush and dispose the log writer.",
                            flushError,
                            disposeError);
                    }

                    throw;
                }

                writer.Dispose();
            }
        }
    }
}