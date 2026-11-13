using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace dnSpy.MCP.Logging {
    public static class Log {
        [ThreadStatic]
        private static bool writing;
        private static readonly object lifecycleGate = new object();
        private static GlobalState? currentState;
        private static bool initializing;
        private static bool closing;
        private const string DefaultCategory = nameof(Log);

        public static void I(string template, params object?[] args) =>
            Write(() => For(DefaultCategory).LogInformation(template, args));

        public static void D(string template, params object?[] args) =>
            Write(() => For(DefaultCategory).LogDebug(template, args));

        public static void W(string template, params object?[] args) =>
            Write(() => For(DefaultCategory).LogWarning(template, args));

        public static void E(string template, params object?[] args) =>
            Write(() => For(DefaultCategory).LogError(template, args));

        public static void E(Exception? exception, string template, params object?[] args) =>
            Write(() => For(DefaultCategory).LogError(exception, template, args));

        public static void I<T>(string eventName, string template, params object?[] args) =>
            Write(() => For<T>().LogInformation(new EventId(0, eventName), template, args));

        public static void D<T>(string eventName, string template, params object?[] args) =>
            Write(() => For<T>().LogDebug(new EventId(0, eventName), template, args));

        public static void W<T>(string eventName, string template, params object?[] args) =>
            Write(() => For<T>().LogWarning(new EventId(0, eventName), template, args));

        public static void E<T>(string eventName, string template, params object?[] args) =>
            Write(() => For<T>().LogError(new EventId(0, eventName), template, args));

        public static void E<T>(string eventName, Exception? exception, string template, params object?[] args) =>
            Write(() => For<T>().LogError(new EventId(0, eventName), exception, template, args));

        public static void Initialize(LogConfiguration configuration) {
            if (configuration is null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            InitializeCore(() => {
                var runtime = configuration.CreateRuntime();
                try {
                    return new GlobalState(runtime.LoggerFactory, runtime);
                } catch (Exception creationError) {
                    try {
                        runtime.Dispose();
                    } catch (Exception cleanupError) {
                        throw new AggregateException(
                            "Failed to initialize logging and release its runtime.",
                            creationError,
                            cleanupError);
                    }
                    throw;
                }
            });
        }

        public static void Initialize(ILoggerFactory factory) {
            if (factory is null) {
                throw new ArgumentNullException(nameof(factory));
            }

            InitializeCore(() => new GlobalState(factory, owner: null));
        }

        private static void InitializeCore(Func<GlobalState> createState) {
            lock (lifecycleGate) {
                if (initializing || closing || currentState is not null) {
                    throw new InvalidOperationException(
                        "Logging is already initialized or changing its lifecycle state.");
                }
                initializing = true;
            }

            GlobalState state;
            try {
                state = createState();
            } catch {
                lock (lifecycleGate) {
                    initializing = false;
                }
                throw;
            }

            lock (lifecycleGate) {
                currentState = state;
                initializing = false;
            }
        }

        public static void CloseAndFlush() {
            GlobalState? state;
            lock (lifecycleGate) {
                if (initializing) {
                    throw new InvalidOperationException(
                        "Logging cannot close while initialization is in progress.");
                }
                if (closing || currentState is null) {
                    return;
                }

                state = currentState;
                currentState = null;
                state.MarkClosed();
                closing = true;
            }

            try {
                state.Owner?.Dispose();
            } finally {
                lock (lifecycleGate) {
                    closing = false;
                }
            }
        }

        public static ILogger For(string category) {
            if (category is null) {
                throw new ArgumentNullException(nameof(category));
            }

            return Resolve<ILogger>(factory => factory.CreateLogger(category), NullLogger.Instance);
        }

        public static ILogger<T> For<T>() =>
            Resolve<ILogger<T>>(factory => factory.CreateLogger<T>(), NullLogger<T>.Instance);

        private static TLogger Resolve<TLogger>(Func<ILoggerFactory, TLogger> createLogger, TLogger fallback) {
            GlobalState? state;
            lock (lifecycleGate) {
                state = currentState;
            }

            if (state is null) {
                return fallback;
            }

            try {
                return createLogger(state.Factory);
            } catch (ObjectDisposedException) when (state.IsClosed) {
                return fallback;
            }
        }

        private static void Write(Action write) {
            if (writing) {
                return;
            }
            writing = true;
            try {
                write();
            } catch (Exception error) {
                try {
                    Console.Error.WriteLine(error);
                } catch {
                }
            } finally {
                writing = false;
            }
        }

        private sealed class GlobalState {
            private int closed;
            public ILoggerFactory Factory { get; }
            public IDisposable? Owner { get; }
            public bool IsClosed => Volatile.Read(ref closed) != 0;

            public GlobalState(ILoggerFactory factory, IDisposable? owner) {
                Factory = factory;
                Owner = owner;
            }

            public void MarkClosed() => Volatile.Write(ref closed, 1);
        }
    }
}
