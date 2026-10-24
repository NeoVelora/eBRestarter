namespace eBRestarter.Core.Application.Ports.Outbound.Security;

public interface IOutboundPortExecutableSignatureVerifier
{
    bool IsTrustedPublisher(string filePath);
}
