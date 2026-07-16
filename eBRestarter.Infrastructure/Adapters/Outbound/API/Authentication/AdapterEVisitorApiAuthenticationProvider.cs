using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Authentication;
using eBRestarter.Infrastructure.Api;
using eBRestarter.Infrastructure.Api.Interfaces;
using eBRestarter.Infrastructure.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.ObjectArchetypes.Model;

namespace eBRestarter.Infrastructure.Adapters.Outbound.API.Authentication;

public sealed class AdapterEVisitorApiAuthenticationProvider(IRestClient restClientPort) : IOutboundPortApiAuthenticationProvider
{
    private readonly IRestClient _restClientPort = restClientPort;

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


