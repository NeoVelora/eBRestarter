using System.DirectoryServices.AccountManagement;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.WindowsOS.Authentication;

public sealed class AdapterWindowsActiveDirectoryProvider : IOutboundPortActiveDirectoryProvider
{
    public bool ValidateCredentials(DirectoryContextScope contextScope, string domain, string username, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        var contextType = contextScope switch
        {
            DirectoryContextScope.Domain => ContextType.Domain,
            DirectoryContextScope.ApplicationDirectory => ContextType.ApplicationDirectory,
            _ => ContextType.Machine
        };

        string? targetDomain = string.IsNullOrWhiteSpace(domain) ? null : domain;

        using var context = new PrincipalContext(contextType, targetDomain);

        return context.ValidateCredentials(username, password);
    }
}
