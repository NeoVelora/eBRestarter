using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Adapters.Authentication;

public sealed class WindowsActiveDirectoryProviderAdapter : IActiveDirectoryPort
{
    public bool ValidateCredentials(ContextType contextType, string domain, string username, string password)
    {
        using var context = new PrincipalContext(contextType, domain);

        return context.ValidateCredentials(username, password);
    }
}
