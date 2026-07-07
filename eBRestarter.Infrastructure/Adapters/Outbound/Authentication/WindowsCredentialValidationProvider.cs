using eBRestarter.Core.Application.Enums;
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
            var contextScope = DirectoryContextScope.Machine;

            if (!string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                contextScope = DirectoryContextScope.Domain;
            }

            return _adProviderPort.ValidateCredentials(contextScope, domain, username, password);
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


