using System;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Update;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;

using eBRestarter.Infrastructure.BehavioralComponents.Clients.Http;
using eBRestarter.Infrastructure.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.ObjectArchetypes.Models;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.API.Authentication;

public sealed class AdapterEVisitorApiAuthenticationProvider : IOutboundPortApiAuthenticationProvider
{
    private const string ConnectionSuccessfulMessage = "Connection successful!";
    private const string ErrorPrefix = "Error: ";
    private const string InvalidCredentialsErrorMessage = "Invalid username or API key.";
    private const int RequestTimeoutSeconds = 10;

    private readonly IRestClient _restClientPort;


    public AdapterEVisitorApiAuthenticationProvider(IRestClient restClientPort)
    {
        ArgumentNullException.ThrowIfNull(restClientPort);

        _restClientPort = restClientPort;
    }


    public Task<VerificationResult> VerifyCredentialsAsync(string username, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(apiKey);

        return VerifyCredentialsCoreAsync(username, apiKey);
    }

    private async Task<VerificationResult> VerifyCredentialsCoreAsync(string username, string apiKey)
    {
        var request = new ApiRequest
        {
            Url = ApiWebLinks.HourlyEarnings,
            Username = username,
            Password = apiKey,
            TimeoutSeconds = RequestTimeoutSeconds
        };

        var response = await _restClientPort.ExecuteGetAsync(request).ConfigureAwait(false);

        return response switch
        {
            { IsSuccess: true } => new VerificationResult(true, ConnectionSuccessfulMessage),
            { StatusCode: ResponseCode.HttpRE401 } => new VerificationResult(false, InvalidCredentialsErrorMessage),
            _ => new VerificationResult(false, $"{ErrorPrefix}{response.ErrorMessage}")
        };
    }
}
