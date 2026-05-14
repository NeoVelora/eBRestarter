namespace eBRestarter.Core.Application.Interfaces.Authentication;

public record VerificationResult(bool IsValid, string Message);

public interface IApiAuthenticationService
{
    Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey);
}
