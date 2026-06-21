using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Api;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;

namespace eBRestarter.Infrastructure.Services.RestSharp;

public class RestSharpClientHandler(ILogger<RestSharpClientHandler> logger, HttpMessageHandler? httpMessageHandler = null) : IRestClientUseCase
{
    private readonly ILogger<RestSharpClientHandler> _logger = logger;
    private readonly HttpMessageHandler? _httpMessageHandler = httpMessageHandler;

    public async Task<ApiResponse> ExecuteGetAsync(ApiRequest request)
    {
        using var client = CreateClient(request);

        var restRequest = new RestRequest();

        try
        {
            var response = await client.ExecuteGetAsync(restRequest);
            return MapResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim asynchronen API-Aufruf: {Url}", request.Url);
            return MapExceptionToResponse(ex);
        }
    }

    public ApiResponse ExecuteGet(ApiRequest request)
    {
        using var client = CreateClient(request);

        var restRequest = new RestRequest();

        try
        {
            var response = client.ExecuteGet(restRequest);
            return MapResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim synchronen API-Aufruf: {Url}", request.Url);
            return MapExceptionToResponse(ex);
        }
    }

    private RestClient CreateClient(ApiRequest model)
    {
        var options = new RestClientOptions(model.Url)
        {
            Timeout = TimeSpan.FromSeconds(model.TimeoutSeconds)
        };

        if (_httpMessageHandler is not null)
        {
            options.ConfigureMessageHandler = _ => _httpMessageHandler;
        }

        if (!string.IsNullOrWhiteSpace(model.Username) && !string.IsNullOrWhiteSpace(model.Password))
        {
            options.Authenticator = new HttpBasicAuthenticator(model.Username, model.Password);
        }

        return new RestClient(options);
    }

    private ApiResponse MapResponse(RestResponse response)
    {
        if (response.IsSuccessful)
        {
            var rateCode = CheckRateLimit(response);
            return new ApiResponse
            {
                IsSuccess = true,
                Content = response.Content,
                StatusCode = rateCode
            };
        }
        if (response.ResponseStatus == ResponseStatus.TimedOut)
        {
            _logger.LogWarning("API error: Local timeout at {Url}", response.Request.Resource);

            return new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResponseCode.HTTPTimeout,
                ErrorMessage = "The request has exceeded the time limit"
            };
        }

        var errorCode = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => ResponseCode.HttpRE401,
            HttpStatusCode.TooManyRequests => ResponseCode.HttpRE429,
            HttpStatusCode.InternalServerError => ResponseCode.InternalServerError,
            HttpStatusCode.RequestTimeout => ResponseCode.HTTPTimeout,
            HttpStatusCode.GatewayTimeout => ResponseCode.HTTPTimeout,
            _ => ResponseCode.NoConnectionToServer
        };

        _logger.LogWarning("API Fehler: {Status} - {Msg}", response.StatusCode, response.ErrorMessage);

        return new ApiResponse
        {
            IsSuccess = false,
            StatusCode = errorCode,
            ErrorMessage = response.ErrorMessage ?? response.StatusDescription
        };
    }

    private static ResponseCode CheckRateLimit(RestResponse response)
    {
        var header = response.Headers?.FirstOrDefault(h => h.Name == "X-Ratelimit-Remaining")?.Value?.ToString();

        if (int.TryParse(header, out int remaining) && remaining <= 10)
        {
            return ResponseCode.RequestLimit;
        }

        return ResponseCode.Success;

    }

    private static ApiResponse MapExceptionToResponse(Exception ex)
    {
        var code = ex switch
        {
            TimeoutException => ResponseCode.HTTPTimeout,

            TaskCanceledException => ResponseCode.HTTPTimeout,

            HttpRequestException httpEx when httpEx.Message.Contains("429") => ResponseCode.HttpRE429,

            HttpRequestException httpEx when httpEx.Message.Contains("401") => ResponseCode.HttpRE401,

            _ => ResponseCode.GeneralExceptionError
        };

        return new ApiResponse
        {
            IsSuccess = false,
            StatusCode = code,
            ErrorMessage = ex.Message
        };
    }
}

