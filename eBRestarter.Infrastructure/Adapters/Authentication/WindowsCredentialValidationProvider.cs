using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Adapters.Authentication;

public sealed class WindowsCredentialValidationProvider(IActiveDirectoryProviderOutboundPort adProviderPort) : ICredentialValidationProviderOutboundPort
{
    private readonly IActiveDirectoryProviderOutboundPort _adProviderPort = adProviderPort;

    public bool ValidateCredentials(string username, string domain, string password)
    {
        try
        {
            var contextType = ContextType.Machine;

            if (!string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                contextType = ContextType.Domain;
            }

            return _adProviderPort.ValidateCredentials(contextType, domain, username, password);
        }
        catch (PrincipalServerDownException)
        {
            throw new InvalidOperationException("PrincipalServerDown");
        }
        catch (Exception)
        {
            return false;
        }
    }
}


