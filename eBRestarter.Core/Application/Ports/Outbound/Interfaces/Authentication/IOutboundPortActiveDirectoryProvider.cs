using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface IOutboundPortActiveDirectoryProvider
{
    bool ValidateCredentials(DirectoryContextScope contextScope, string domain, string username, string password);
}

