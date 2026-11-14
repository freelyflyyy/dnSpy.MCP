using Microsoft.Extensions.Logging;

namespace dnSpy.MCP.Logging.Providers {
    public sealed class SinkLoggerProvider
        : ILoggerProvider, ISupportExternalScope {

        private readonly ILogSink sink;
        private readonly bool ownsSink;
        private readonly object gate = new object();
        private IExternalScopeProvider scopeProvider =
            new LoggerExternalScopeProvider();
        private int dispose;

        public SinkLoggerProvider(
            ILogSink sink,
            bool ownsSink = false) {

            this.sink = sink
                ?? throw new ArgumentNullException(nameof(sink));

            this.ownsSink = ownsSink;
        }

        internal bool IsDisposed => Volatile.Read(ref dispose) != 0;
        internal IExternalScopeProvider ScopeProvider => Volatile.Read(ref scopeProvider);
        public ILogger CreateLogger(string categoryName) {
            if (categoryName is null) {
                throw new ArgumentNullException(nameof(categoryName));
            }

            if (IsDisposed) {
                throw new ObjectDisposedException(nameof(SinkLoggerProvider));
            }
            return new SinkLogger(categoryName, this);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) {
            if (scopeProvider is null) {
                throw new ArgumentNullException(nameof(scopeProvider));
            }

            Volatile.Write(ref this.scopeProvider, scopeProvider);
        }

        internal void Write(LogEntry entry) {
            lock(gate) {
                if (dispose == 0) {
                    sink.Write(entry);
                }
            }
        }

        public void Dispose() {
            lock (gate) {
                if (dispose != 0) {
                    return;
                }

                Volatile.Write(ref dispose, 1);

                if (ownsSink && sink is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }
    }
}