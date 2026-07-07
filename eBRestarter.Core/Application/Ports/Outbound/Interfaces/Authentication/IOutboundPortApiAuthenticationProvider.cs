using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface IOutboundPortApiAuthenticationProvider
{
    Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey);
}

