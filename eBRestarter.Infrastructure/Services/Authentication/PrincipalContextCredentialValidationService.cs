using eBRestarter.Core.Application.Interfaces.Authentication;
using System;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Services.Authentication
{
    public class PrincipalContextCredentialValidationService : ICredentialValidationService
    {
        #region PublicAndProtectedMethods

        public bool ValidateCredentials(string username, string domain, string password)
        {
            try
            {
                ContextType contextType = ContextType.Machine;

                if (!string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                {
                    contextType = ContextType.Domain;
                }

                using (var context = new PrincipalContext(contextType, domain))
                {
                    return context.ValidateCredentials(username, password);
                }
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

        #endregion
    }
}
