namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface ICredentialValidationPort
{
    bool ValidateCredentials(string username, string domain, string password);
}


