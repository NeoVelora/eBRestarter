using eBRestarter.Core.Application.Interfaces.Authentication;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Services.Authentication;

public class PrincipalContextCredentialValidationService(IActiveDirectoryService adService) : ICredentialValidationService
{
    private readonly IActiveDirectoryService _adService = adService;

    public bool ValidateCredentials(string username, string domain, string password)
    {
        try
        {
            ContextType contextType = ContextType.Machine;

            if (!string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                contextType = ContextType.Domain;
            }

            return _adService.ValidateCredentials(contextType, domain, username, password);
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
