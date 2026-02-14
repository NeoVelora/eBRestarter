namespace eBRestarter.Core.Application.Interfaces.Authentication
{
    public interface ICredentialValidationService
    {
        #region PublicMethods

        bool ValidateCredentials(string username, string domain, string password);

        #endregion
    }
}
