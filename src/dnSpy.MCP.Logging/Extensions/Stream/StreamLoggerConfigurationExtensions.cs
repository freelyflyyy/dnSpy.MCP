using dnSpy.MCP.Logging.Extensions.TextWriter;
using System.Runtime;
using System.Text;

namespace dnSpy.MCP.Logging.Extensions.Stream {
    public static class StreamLoggerConfigurationExtensions {
        public static LogConfiguration Stream(
            this LogWriteToConfiguration writeTo,
            System.IO.Stream stream,
            StreamSinkOptions? options = null) {

            if (stream is null) {
                throw new ArgumentNullException(nameof(stream));
            }

            return writeTo.Stream(
                _ => stream,
                true,
                options);
        }
        public static LogConfiguration Stream(
            this LogWriteToConfiguration writeTo,
            Func<IServiceProvider, System.IO.Stream> factory,
            bool leaveOpen,
            StreamSinkOptions? options = null
            ) {

            if (writeTo is null) {
                throw new ArgumentNullException(nameof(writeTo));
            }

            if (factory is null) {
                throw new ArgumentNullException(nameof(factory));
            }

            var settings = options ?? new StreamSinkOptions();

            var encoding = settings.Encoding
                ?? throw new ArgumentException(
                    "An encoding is required.",
                    nameof(options));

            var bufferSize = settings.BufferSize;

            if (bufferSize <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "BufferSize must be positive.");
            }

            var encodingSnapshot = (Encoding)encoding.Clone();

            var writerOptions = new TextWriterSinkOptions {
                Formatter = settings.Formatter,
                AutoFlush = settings.AutoFlush
            };

            return writeTo.TextWriter(services => {
                var stream = factory(services)
                    ?? throw new InvalidOperationException(
                        "The stream returned a null.");

                try {
                    return new StreamWriter(
                        stream,
                        encodingSnapshot,
                        bufferSize,
                        leaveOpen);
                } catch(Exception creationError) {
                    if (!leaveOpen) {
                        try {
                            stream.Dispose();
                        }catch(Exception cleanupError) {
                            throw new AggregateException(
                                "Failed to create the stream writer and release its stream.",
                                creationError,
                                cleanupError);
                        }
                    }
                    throw;
                }
            }, leaveOpen: false, options: writerOptions);
        }
    }
}