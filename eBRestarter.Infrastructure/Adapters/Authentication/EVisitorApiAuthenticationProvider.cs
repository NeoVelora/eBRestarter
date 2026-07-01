using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Infrastructure.Api;
using eBRestarter.Infrastructure.Network;

namespace eBRestarter.Infrastructure.Adapters.Authentication;

public sealed class EVisitorApiAuthenticationProvider(IRestClientPort restClientPort) : IApiAuthenticationProviderOutboundPort
{
    private readonly IRestClientPort _restClientPort = restClientPort;

    public async Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey)
    {
        var request = new ApiRequest
        {
            Url = ApiWebLinks.HourlyEarnings,
            Username = username,
            Password = apiKey,
            TimeoutSeconds = 10
        };

        var response = await _restClientPort.ExecuteGetAsync(request);

        if (response.IsSuccess)
        {
            return new VerificationResult(true, "Connection successful!");
        }
        else if (response.StatusCode == ResponseCode.HttpRE401)
        {
            return new VerificationResult(false, "Invalid username or API key.");
        }
        else
        {
            return new VerificationResult(false, $"Error: {response.ErrorMessage}");
        }
    }
}


