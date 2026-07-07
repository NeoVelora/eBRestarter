namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortCredentialValidationProvider
{
    bool ValidateCredentials(string username, string domain, string password);
}


