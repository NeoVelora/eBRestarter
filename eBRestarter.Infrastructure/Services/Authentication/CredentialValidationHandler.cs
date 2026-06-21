using eBRestarter.Core.Application.Interfaces.Authentication;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Services.Authentication;

public class CredentialValidationHandler(IActiveDirectoryUseCase adUseCase) : ICredentialValidationUseCase
{
    private readonly IActiveDirectoryUseCase _adUseCase = adUseCase;

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

