using System.DirectoryServices.AccountManagement;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface IActiveDirectoryProviderOutboundPort
{
    bool ValidateCredentials(ContextType contextType, string domain, string username, string password);
}

