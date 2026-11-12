using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

        private static readonly object bindingGate = new object();
        private static RuntimeBinding? currentBinding;

        private const string DefaultCategory = "dnSpy.MCP";

        public static void I(
            string messageTemplate,
            params object?[] args) =>
            For(DefaultCategory).LogInformation(messageTemplate, args);

        public static void D(
            string messageTemplate,
            params object?[] args) =>
            For(DefaultCategory).LogDebug(messageTemplate, args);

        public static void W(
            string messageTemplate,
            params object?[] args) =>
            For(DefaultCategory).LogWarning(messageTemplate, args);

        public static void E(
            string messageTemplate,
            params object?[] args) =>
            For(DefaultCategory).LogError(messageTemplate, args);

        public static void E(
            Exception? exception,
            string messageTemplate,
            params object?[] args) =>
            For(DefaultCategory).LogError(
                exception,
                messageTemplate,
                args);

        public static IDisposable Use(LoggingRuntime runtime) {
            if (runtime is null) {
                throw new ArgumentNullException(nameof(runtime));
            }

            return Use(runtime.LoggerFactory);
        }

        public static IDisposable Use(ILoggerFactory factory) {
            if (factory is null) {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (bindingGate) {
                var binding = new RuntimeBinding(factory, currentBinding);
                currentBinding = binding;
                return binding;
            }
        }

        public static ILogger For(string category) {
            if (category is null) {
                throw new ArgumentNullException(nameof(category));
            }

            return Resolve<ILogger>(
                factory => factory.CreateLogger(category),
                NullLogger.Instance);
        }

        public static ILogger<T> For<T>() =>
            Resolve<ILogger<T>>(
                factory => factory.CreateLogger<T>(),
                NullLogger<T>.Instance);

        private static TLogger Resolve<TLogger>(
            Func<ILoggerFactory, TLogger> createLogger,
            TLogger fallback) {

            RuntimeBinding? binding;

            lock (bindingGate) {
                binding = currentBinding;
            }

            if (binding is null) {
                return fallback;
            }

            try {
                return createLogger(binding.Factory);
            } catch (ObjectDisposedException) when (binding.IsDisposed) {
                return fallback;
            }
        }

        private sealed class RuntimeBinding : IDisposable {
            private int disposed;

            public ILoggerFactory Factory { get; }
            public RuntimeBinding? Previous { get; }

            public bool IsDisposed =>
                Volatile.Read(ref disposed) != 0;

            public RuntimeBinding(
                ILoggerFactory factory,
                RuntimeBinding? previous) {

                Factory = factory;
                Previous = previous;
            }

            public void Dispose() {
                lock (bindingGate) {
                    if (IsDisposed) {
                        return;
                    }

                    Volatile.Write(ref disposed, 1);

                    while (currentBinding is not null &&
                           currentBinding.IsDisposed) {

                        currentBinding = currentBinding.Previous;
                    }
                }
            }
        }
    }
}
