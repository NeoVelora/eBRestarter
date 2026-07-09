using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Authentication;

public sealed class AdapterWindowsCredentialValidationProvider(IOutboundPortActiveDirectoryProvider adProviderPort) : IOutboundPortCredentialValidationProvider
{
    private readonly IOutboundPortActiveDirectoryProvider _adProviderPort = adProviderPort;

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


