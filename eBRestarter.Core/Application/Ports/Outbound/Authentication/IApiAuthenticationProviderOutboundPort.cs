using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface IApiAuthenticationProviderOutboundPort
{
    Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey);
}

