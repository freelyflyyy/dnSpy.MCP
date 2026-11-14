using dnSpy.Contracts.Output;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.ToolWindows.App;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;

namespace dnSpy.MCP.Extension.UI {
    [Export(typeof(McpOutput))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class McpOutput {
        internal static readonly Guid PaneGuid = new("9228341C-4C2B-4093-AB30-5B908F31D297");
        private readonly IOutputService outputService;
        private readonly Dispatcher dispatcher;
        [ImportingConstructor]
        public McpOutput(IOutputService outputService) {
            this.outputService = outputService;

            this.dispatcher = System.Windows.Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("The WPF application is not available");
        }

        public void Write(string text, TextColor color = TextColor.Text) {
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
                return;
            }

            dispatcher.BeginInvoke(new Action(() => {
                if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) {
                    return;
                }

                try {
                    var pane = outputService.Create(
                        PaneGuid,
                        "dnSpy.MCP",
                        "text");
                    pane.Write(color,text);
                }catch(Exception ex) {
                    try {
                        Trace.WriteLine(ex);
                    } catch {
                    }
                }
            }));
        }

        public void WriteLine(string text, TextColor color = TextColor.Text) {
            Write(text + Environment.NewLine, color);
        }
    }
}
