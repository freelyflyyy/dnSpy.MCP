namespace dnSpy.MCP.Logging.Extensions.TextWriter {
    public static class TextWriterLoggerConfigurationExtensions {
        public static LogConfiguration TextWriter(
            this LogWriteToConfiguration writeTo,
            System.IO.TextWriter writer,
            TextWriterSinkOptions? options = null) {

            if (writer is null) {
                throw new ArgumentNullException(nameof(writer));
            }

            return writeTo.TextWriter(
                _ => writer,
                leaveOpen: true,
                options: options);
        }

        public static LogConfiguration TextWriter(
            this LogWriteToConfiguration writeTo,
            Func<IServiceProvider, System.IO.TextWriter> factory,
            bool leaveOpen,
            TextWriterSinkOptions? options = null) {

            if (writeTo is null) {
                throw new ArgumentNullException(nameof(writeTo));
            }

            if (factory is null) {
                throw new ArgumentNullException(nameof(factory));
            }

            var settings = options ?? new TextWriterSinkOptions();

            var snapshot = new TextWriterSinkOptions {
                Formatter = settings.Formatter
                    ?? throw new ArgumentException(
                        "A formatter is required.",
                        nameof(options)),
                AutoFlush = settings.AutoFlush
            };

            return writeTo.Sink(services => {
                var writer = factory(services)
                    ?? throw new InvalidOperationException(
                        "The writer factory returned a null.");
                try {
                    return new TextWriterLogSink(
                        writer,
                        leaveOpen,
                        snapshot);
                }catch(Exception creationError) {
                    if (!leaveOpen) {
                        try {
                            writer.Dispose();
                        }catch(Exception cleanupError) {
                            throw new AggregateException(
                                "Failed to create the text writer sink and release its writer.",
                                creationError,
                                cleanupError);
                        }
                    }

                    throw;
                }
            }, ownsSink: true);
        }
    }
}
