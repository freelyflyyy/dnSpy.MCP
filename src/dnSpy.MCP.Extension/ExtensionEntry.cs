using dnSpy.Contracts.Extension;
using dnSpy.MCP.Extension.Logging;
using dnSpy.MCP.Logging;
using System.ComponentModel.Composition;
using System.Runtime.ExceptionServices;

namespace dnSpy.MCP.Extension {
    [ExportExtension]
    public sealed class ExtensionEntry : IExtension {
        private readonly ExtensionHost host;
        private readonly ILogSink logSink;
        private bool initialized;
        private bool exiting;
        public IEnumerable<string> MergedResourceDictionaries => Array.Empty<string>();

        [ImportingConstructor]
        public ExtensionEntry(ExtensionHost host, ILogSink logSink) {
            this.host = host;
            this.logSink = logSink;
        }
        public ExtensionInfo ExtensionInfo => new ExtensionInfo {
            ShortDescription = "Enable MCP integration for dnSpy"
        };
        public void OnEvent(ExtensionEvent eventType, object? data) {
            if (eventType == ExtensionEvent.AppLoaded) {
                Start();
            } else if (eventType == ExtensionEvent.AppExit) {
                Stop();
            }
        }

        private void Start() {
            if (exiting || initialized) {
                return;
            }

            Log.Initialize(new LogConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.DnSpy(logSink));

            initialized = true;
            // Logging has application lifetime, including service startup failures.
            host.Start();
        }

        private void Stop() {
            if (exiting) {
                return;
            }
            exiting = true;

            Exception? stopError = null;
            try {
                host.RequestStop();
            } catch (Exception error) {
                stopError = error;
            }

            try {
                if (initialized) {
                    initialized = false;
                    Log.CloseAndFlush();
                }
            } catch (Exception closeError) {
                if (stopError is not null) {
                    throw new AggregateException(
                        "Service stop and logging shutdown both failed.",
                        stopError,
                        closeError);
                }
                throw;
            }

            if (stopError is not null) {
                ExceptionDispatchInfo.Capture(stopError).Throw();
            }
        }
    }
}
