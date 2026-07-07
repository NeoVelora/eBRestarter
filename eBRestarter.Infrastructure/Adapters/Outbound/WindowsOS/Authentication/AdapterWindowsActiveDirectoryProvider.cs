using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Authentication;

public sealed class AdapterWindowsActiveDirectoryProvider : IOutboundPortActiveDirectoryProvider
{
    public bool ValidateCredentials(DirectoryContextScope contextScope, string domain, string username, string password)
    {
        var contextType = contextScope switch
        {
            DirectoryContextScope.Domain => ContextType.Domain,
            DirectoryContextScope.ApplicationDirectory => ContextType.ApplicationDirectory,
            _ => ContextType.Machine
        };

        using var context = new PrincipalContext(contextType, domain);

        return context.ValidateCredentials(username, password);
    }
}
