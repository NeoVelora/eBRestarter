using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface IOutboundPortApiAuthenticationProvider
{
    Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey);
}

