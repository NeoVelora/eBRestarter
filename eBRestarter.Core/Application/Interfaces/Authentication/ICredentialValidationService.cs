namespace eBRestarter.Core.Application.Interfaces.Authentication;

public interface ICredentialValidationService
{

    bool ValidateCredentials(string username, string domain, string password);
}
