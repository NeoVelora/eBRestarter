using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Authentication;

public interface IOutboundPortActiveDirectoryProvider
{
    bool ValidateCredentials(DirectoryContextScope contextScope, string domain, string username, string password);
}

