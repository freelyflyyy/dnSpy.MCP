using System.IO.Pipes;

#if NET48
using System.Security.Principal;
using System.Security.AccessControl;
#endif

namespace dnSpy.MCP.Extension.Transport {
    internal static class NamedPipeFactory {
        public static NamedPipeServerStream Create(string pipeName) {
#if NET48
            using var identity = WindowsIdentity.GetCurrent();

            var user = identity.User
                ?? throw new InvalidOperationException("The current Windows user SID is unavailable.");

            var owner = identity.Owner
                ?? throw new InvalidOperationException("The current Windows owner SID is unavailable.");

            var security = new PipeSecurity();
            security.SetAccessRuleProtection(
                isProtected: true,
                preserveInheritance: false);

            security.SetOwner(owner);
            security.AddAccessRule(new PipeAccessRule(
                user,
                PipeAccessRights.FullControl,
                AccessControlType.Allow));

            return new NamedPipeServerStream(
               pipeName,
               PipeDirection.InOut,
               1,
               PipeTransmissionMode.Byte,
               PipeOptions.Asynchronous,
               4096,
               4096,
               security);
#else
            return new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
#endif
        }
    }
}
