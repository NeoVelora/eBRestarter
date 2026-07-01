namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface ICredentialValidationProviderOutboundPort
{
    bool ValidateCredentials(string username, string domain, string password);
}


