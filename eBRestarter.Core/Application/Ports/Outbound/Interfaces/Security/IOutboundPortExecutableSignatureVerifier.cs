namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;

public interface IOutboundPortExecutableSignatureVerifier
{
    bool IsTrustedPublisher(string filePath);
}
