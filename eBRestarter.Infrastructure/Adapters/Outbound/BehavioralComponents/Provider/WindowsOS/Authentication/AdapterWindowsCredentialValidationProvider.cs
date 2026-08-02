using System.DirectoryServices.AccountManagement;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS.Authentication;

public sealed class AdapterWindowsCredentialValidationProvider(IOutboundPortActiveDirectoryProvider adProviderPort) : IOutboundPortCredentialValidationProvider
{
    private readonly IOutboundPortActiveDirectoryProvider _adProviderPort = adProviderPort ?? throw new ArgumentNullException(nameof(adProviderPort));

    public bool ValidateCredentials(string username, string domain, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            var contextScope = !string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                ? DirectoryContextScope.Domain
                : DirectoryContextScope.Machine;

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
