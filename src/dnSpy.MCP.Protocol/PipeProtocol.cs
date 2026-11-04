using System.Buffers.Binary;

namespace dnSpy.MCP.Protocol {
    public static class PipeProtocol {
        public const int Version = 1;
        public const string Hello = "hello";
        public const string Execute = "execute";
        public const int MaxRequestSize = 256 * 1024; // 256 KiB
        public const int MaxResponseSize = 4 * 1024 * 1024; // 4 MiB
        public const string PipeNamePrefix = "dnSpy-Mcp-";
        public static string GetPipeName(int processId) {
            if (processId <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(processId),
                    processId,
                    "Process ID must be greater than zero.");
            }

            return $"{PipeNamePrefix}{processId}";
        }

        public static async Task<byte[]> ReadMessageAsync(Stream stream, int maxSize, CancellationToken cancellationToken = default) {
            if (maxSize <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(maxSize),
                    maxSize,
                    "Maximum message size must be greater than zero.");
            }

            var header = new byte[4];
            await ReadExactlyAsync(stream, header, cancellationToken);

            var length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length <= 0 || length > maxSize) {
                throw new InvalidDataException(
                    $"Request size must be between 1 byte and {maxSize} bytes.");
            }

            var payload = new byte[length];
            await ReadExactlyAsync(stream, payload, cancellationToken);

            return payload;
        }

        public static async Task WriteMessageAsync(
            Stream stream,
            byte[] payload,
            int maxSize,
            CancellationToken cancellationToken = default){

            if (maxSize <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(maxSize),
                    maxSize,
                    "Maximum message size must be greater than zero.");
            }

            if (payload is null) {
                throw new ArgumentNullException(nameof(payload));
            }

            if (payload.Length == 0 || payload.Length > maxSize) {
                throw new InvalidDataException($"Message size must be between 1 byte and {maxSize} bytes.");
            }

            var header = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

            await stream.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken) {
            int totalRead = 0;
            while(totalRead < buffer.Length) {
                int read = await stream.ReadAsync(
                    buffer,
                    totalRead,
                    buffer.Length - totalRead,
                    cancellationToken).ConfigureAwait(false);

                if (read == 0) {
                    throw new EndOfStreamException(
                        "The stream ended before all expected bytes were read.");
                }

                totalRead += read;
            }
        }
    }
}
