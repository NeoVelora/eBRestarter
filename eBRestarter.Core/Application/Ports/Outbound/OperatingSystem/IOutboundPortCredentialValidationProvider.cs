namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOutboundPortCredentialValidationProvider
{
    bool ValidateCredentials(string username, string domain, string password);
}


