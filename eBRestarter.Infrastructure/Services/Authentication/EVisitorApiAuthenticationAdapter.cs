using eBRestarter.Core.Application.Interfaces.Authentication;
using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Api;
using eBRestarter.Core.Application.Constants;

namespace eBRestarter.Infrastructure.Services.Authentication;

public class EVisitorApiAuthenticationAdapter(IRestClientUseCase restClientUseCase) : IApiAuthenticationUseCase
{
    private readonly IRestClientUseCase _restClientUseCase = restClientUseCase;

    public async Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey)
    {
        var request = new ApiRequest
        {
            Url = ApiWebLinks.HourlyEarnings,
            Username = username,
            Password = apiKey,
            TimeoutSeconds = 10
        };

        var response = await _restClientUseCase.ExecuteGetAsync(request);

        if (response.IsSuccess)
        {
            return new VerificationResult(true, "Verbindung erfolgreich!");
        }
        else if (response.StatusCode == ResponseCode.HttpRE401)
        {
            return new VerificationResult(false, "Benutzername oder API-SchlÃ¼ssel falsch.");
        }
        else
        {
            return new VerificationResult(false, $"Fehler: {response.ErrorMessage}");
        }
    }
}


