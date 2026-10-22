using dnSpy.MCP.Extension.Transport;
using dnSpy.MCP.Extension.UI;
using System.ComponentModel.Composition;
using System.Windows.Threading;

namespace dnSpy.MCP.Extension {
    [Export(typeof(ExtensionHost))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ExtensionHost {
        private readonly PipeServer server;
        private readonly McpOutput output;
        private readonly Dispatcher dispatcher;

        private CancellationTokenSource? stopSource;
        private Task completion = Task.CompletedTask;

        public bool IsRunning { get; private set; }
        public string PipeName => server.PipeName;

        [ImportingConstructor]
        public ExtensionHost(PipeServer server, McpOutput output) {
            this.server = server;
            this.output = output;

            dispatcher = System.Windows.Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("The WPF application is unavailable.");
        }

        public void Start() {
            dispatcher.VerifyAccess();

            if (IsRunning) {
                return;
            }

            IsRunning = true;

            var source = new CancellationTokenSource();
            stopSource = source;
            completion = RunObservedAsync(source);
        }

        public void RequestStop() {
            dispatcher.VerifyAccess();
            stopSource?.Cancel();
        }

        public Task StopAsync() {
            RequestStop();
            return completion;
        }

        private async Task RunObservedAsync(CancellationTokenSource source) {
            var message = "Pipe server stopped.";
            try {
                await Task.Run(() => server.RunAsync(source.Token));
            } catch (OperationCanceledException)
                when (source.IsCancellationRequested) {
            } catch (Exception ex) {
                message =
                    $"Pipe server exited unexpectedly: {ex.GetType().Name}"
                    + Environment.NewLine
                    + ex.Message;

            } finally {
                stopSource = null;
                IsRunning = false;
                source.Dispose();
            }

            output.WriteResult(message);
        }
    }
}
