namespace eBRestarter.Core.Application.Interfaces.Authentication;

public interface ICredentialValidationUseCase
{
    bool ValidateCredentials(string username, string domain, string password);
}
