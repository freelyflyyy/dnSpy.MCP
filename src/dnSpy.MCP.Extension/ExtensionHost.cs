using dnSpy.MCP.Extension.Transport;
using dnSpy.MCP.Extension.UI;
using dnSpy.MCP.Logging;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;

namespace dnSpy.MCP.Extension {
    [Export(typeof(ExtensionHost))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ExtensionHost {
        private readonly PipeServer server;
        private readonly Dispatcher dispatcher;

        private CancellationTokenSource? stopSource;
        private Task completion = Task.CompletedTask;

        public ServiceState State { get; private set; } = ServiceState.Stopped;
        public string? LastError { get; private set; }
        public string PipeName => server.PipeName;

        public bool CanStart => State is ServiceState.Stopped or ServiceState.Faulted;
        public bool CanStop => State is ServiceState.Starting or ServiceState.Running;

        [ImportingConstructor]
        public ExtensionHost(PipeServer server) {
            this.server = server;

            dispatcher = System.Windows.Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("The WPF application is unavailable.");
        }

        public void Start() {
            dispatcher.VerifyAccess();

            if (!CanStart) {
                return;
            }

            var source = new CancellationTokenSource();
            stopSource = source;
            LastError = null;
            State = ServiceState.Starting;

            Log.i("Service.Starting", "MCP service is starting.");
            completion = RunObservedAsync(source);
        }

        public void RequestStop() {
            dispatcher.VerifyAccess();
            if (!CanStop || stopSource is null) {
                return;
            }

            State = ServiceState.Stopping;
            Log.i("Service.Stopping", "Accepted scripts may still be running.");
            stopSource.Cancel();
        }

        public Task StopAsync() {
            RequestStop();
            return completion;
        }

        private void NotifyListening(CancellationTokenSource source) {
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
                return;
            }
            dispatcher.BeginInvoke(new Action(() => {
                // Handle the listening notification
                if (!ReferenceEquals(source, stopSource)
                    || State != ServiceState.Starting
                    || source.IsCancellationRequested) {
                    return;
                }

                State = ServiceState.Running;
                Log.i("Service.Running", $"MCP service is running: {PipeName}");
            }));
        }

        private async Task RunObservedAsync(CancellationTokenSource source) {
            Exception? failure = null;

            try {
                await Task.Run(() => server.RunAsync(
                    source.Token, () => NotifyListening(source)));
            }catch(OperationCanceledException)
                when (source.IsCancellationRequested) {
                // Normal cancellation
            } catch (Exception ex) {
                failure = ex;
            } finally {
                if (ReferenceEquals(stopSource, source)) {
                    stopSource = null;

                    LastError = failure is null
                        ? null
                        : $"{failure.GetType().Name}: {failure.Message}";

                    State = failure is null
                        ? ServiceState.Stopped
                        : ServiceState.Faulted;
                }

                source.Dispose();
            }

            if (failure is null) {
                Log.i("Service.Stopped", "MCP service stopped.");
            } else {
                Log.e("Service.Faulted", $"MCP service failed: {LastError}");
            }
        }
    }
}
