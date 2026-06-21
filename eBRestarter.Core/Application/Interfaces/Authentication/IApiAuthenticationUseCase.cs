namespace eBRestarter.Core.Application.Interfaces.Authentication;

public record VerificationResult(bool IsValid, string Message);

public interface IApiAuthenticationUseCase
{
    Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey);
}
