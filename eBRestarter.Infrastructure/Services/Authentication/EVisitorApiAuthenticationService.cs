using eBRestarter.Core.Application.Interfaces.Authentication;
using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Api;
using eBRestarter.Core.Application.Constants;

namespace eBRestarter.Infrastructure.Services.Authentication;

public class EVisitorApiAuthenticationService(IRestClientService restClient) : IApiAuthenticationService
{

    private readonly IRestClientService _restClient = restClient;

    public async Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey)
    {

        var request = new ApiRequest
        {
            Url = ApiWebLinks.HourlyEarnings,
            Username = username,
            Password = apiKey,
            TimeoutSeconds = 10
        };

        var response = await _restClient.ExecuteGetAsync(request);

        if (response.IsSuccess)
        {
            return new VerificationResult(true, "Verbindung erfolgreich!");
        }
        else if (response.StatusCode == ResponseCode.HttpRE401)
        {
            return new VerificationResult(false, "Benutzername oder API-Schlüssel falsch.");
        }
        else
        {
            return new VerificationResult(false, $"Fehler: {response.ErrorMessage}");
        }
    }
}
