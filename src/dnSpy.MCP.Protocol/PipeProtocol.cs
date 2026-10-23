using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace dnSpy.MCP.Protocol {
    public static class PipeProtocol {
        public const int MaxRequestSize = 256 * 1024; // 256 KiB
        public const int MaxResponseSize = 4 * 1024 * 1024; // 4 MiB

        public static string GetPipeName(int processId) {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
            return $"dnSpy-Mcp-{processId}";
        }

        public static async Task<byte[]> ReadMessageAsync(Stream stream, int maxSize, CancellationToken cancellationToken = default) {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSize);

            var header = new byte[4];
            await stream.ReadExactlyAsync(header, cancellationToken);

            var length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length <= 0 || length > maxSize) {
                throw new InvalidDataException(
                    $"Request size must be between 1 byte and {maxSize} bytes.");
            }

            var payload = new byte[length];
            await stream.ReadExactlyAsync(payload, cancellationToken);

            return payload;
        }

        public static async Task WriteMessageAsync(
            Stream stream,
            byte[] payload,
            int maxSize,
            CancellationToken cancellationToken = default){

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSize);
            ArgumentNullException.ThrowIfNull(payload);

            if (payload.Length == 0 || payload.Length > maxSize) {
                throw new InvalidDataException($"Message size must be between 1 byte and {maxSize} bytes.");
            }

            var header = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

            await stream.WriteAsync(header, cancellationToken);
            await stream.WriteAsync(payload, cancellationToken);
        }
    }
}
