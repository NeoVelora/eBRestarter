namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

using eBRestarter.Core.Application.Models.Records;

public interface IApiAuthenticationPort
{
    Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey);
}

