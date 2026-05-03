using eBRestarter.Core.Application.Interfaces.Authentication;
using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Infrastructure.Services.Authentication;

public class WindowsActiveDirectoryService : IActiveDirectoryService
{
    public bool ValidateCredentials(ContextType contextType, string domain, string username, string password)
    {
        using var context = new PrincipalContext(contextType, domain);

        return context.ValidateCredentials(username, password);
    }
}