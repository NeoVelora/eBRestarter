using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Adapters.Authentication;

public sealed class WindowsCredentialValidationAdapter(IActiveDirectoryPort adUseCase) : ICredentialValidationPort
{
    private readonly IActiveDirectoryPort _adUseCase = adUseCase;

    public bool ValidateCredentials(string username, string domain, string password)
    {
        try
        {
            var contextType = ContextType.Machine;

            if (!string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                contextType = ContextType.Domain;
            }

            return _adUseCase.ValidateCredentials(contextType, domain, username, password);
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


