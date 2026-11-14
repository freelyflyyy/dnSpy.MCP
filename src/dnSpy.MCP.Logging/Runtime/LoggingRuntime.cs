using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging {
    public sealed class LoggingRuntime : IDisposable {
        private readonly ILoggerFactory loggerFactory;
        private readonly IDisposable owner;
        private int disposed;

        internal LoggingRuntime(ILoggerFactory loggerFactory, IDisposable owner) {
            this.loggerFactory = loggerFactory
                ?? throw new ArgumentNullException(nameof(loggerFactory));

            this.owner = owner
                ?? throw new ArgumentNullException(nameof(owner));
        }

        public ILoggerFactory LoggerFactory {
            get {
                ThrowIfDisposed();
                return this.loggerFactory;
            }
        }

        public ILogger CreateLogger(string category) {
            if (category is null) {
                throw new ArgumentNullException(nameof(category));
            }

            return LoggerFactory.CreateLogger(category);
        }

        public ILogger<T> CreateLogger<T>() =>
            LoggerFactory.CreateLogger<T>();

        public void Dispose() {
            if (Interlocked.Exchange(ref disposed, 1) != 0) {
                return;
            }

            owner.Dispose();
        }

        private void ThrowIfDisposed() {
            if (Volatile.Read(ref disposed) != 0) {
                throw new ObjectDisposedException(nameof(LoggingRuntime));
            }
        }
    }
}
