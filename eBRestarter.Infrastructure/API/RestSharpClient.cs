using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.Api.Interfaces;
using eBRestarter.Infrastructure.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.ObjectArchetypes.Model;

namespace eBRestarter.Infrastructure.API;

public sealed partial class RestSharpClient(
    ILogger<RestSharpClient> logger,
    IHttpClientFactory httpClientFactory,
    RateLimiter? rateLimiter = null,
    long maxResponseSizeBytes = RestSharpClient.MaxAllowedResponseSizeBytes)
    : eBRestarter.Infrastructure.Api.Interfaces.IRestClient, IDisposable, IAsyncDisposable
{

    public const string HttpClientName = "eBRestarter.RestSharp";

    public const long MaxAllowedResponseSizeBytes = 10 * 1024 * 1024;

    private const string RateLimitQueueFullErrorMessage = "Client-side outbound rate limit queue is full or rate limit exceeded.";
    private const string RateLimitRemainingHeaderName = "X-Ratelimit-Remaining";
    private const int RemainingRateLimitThreshold = 10;
    private const string RequestTimeoutErrorMessage = "The request has exceeded the time limit";
    private const string ResponsePayloadTooLargeErrorMessage = "Response payload exceeds maximum allowed size limit of 10 MB.";


    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<RestSharpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly long _maxResponseSizeBytes = maxResponseSizeBytes > 0 ? maxResponseSizeBytes : MaxAllowedResponseSizeBytes;
    private readonly bool _ownsRateLimiter = rateLimiter == null;
    private readonly RateLimiter _rateLimiter = rateLimiter ?? CreateDefaultRateLimiter();


    public ApiResponse ExecuteGet(ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var lease = _rateLimiter.AttemptAcquire(permitCount: 1);
        if (!lease.IsAcquired)
        {
            LogRateLimitExceeded(request.Url);

            return new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResponseCode.RequestLimit,
                ErrorMessage = RateLimitQueueFullErrorMessage
            };
        }

        using var client = CreateClient(request);

        var restRequest = new RestRequest();

        try
        {
            var response = client.ExecuteGet(restRequest);
            return MapResponse(response);
        }
        catch (Exception exception)
        {
            LogSynchronousInvocationFailed(exception, request.Url);

            return MapExceptionToResponse(exception);
        }
    }

    public async Task<ApiResponse> ExecuteGetAsync(ApiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            LogRateLimitExceeded(request.Url);

            return new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResponseCode.RequestLimit,
                ErrorMessage = RateLimitQueueFullErrorMessage
            };
        }

        using var client = CreateClient(request);

        var restRequest = new RestRequest();

        try
        {
            var response = await client.ExecuteGetAsync(restRequest, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            return MapResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogAsynchronousInvocationFailed(exception, request.Url);

            return MapExceptionToResponse(exception);
        }
    }

    private static ResponseCode CheckRateLimit(RestResponse response)
    {
        var header = response.Headers?.FirstOrDefault(h => h.Name.Equals(RateLimitRemainingHeaderName, StringComparison.OrdinalIgnoreCase))?.Value?.ToString();

        if (int.TryParse(header, out int remaining) && remaining <= RemainingRateLimitThreshold)
        {
            return ResponseCode.RequestLimit;
        }

        return ResponseCode.Success;
    }

    private RestClient CreateClient(ApiRequest request)
    {
        var options = new RestClientOptions(request.Url)
        {
            Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds)
        };

        if (!string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
        {
            options.Authenticator = new HttpBasicAuthenticator(request.Username, request.Password);
        }

        HttpClient pooledHttpClient = _httpClientFactory.CreateClient(HttpClientName);

        return new RestClient(pooledHttpClient, options, disposeHttpClient: false);
    }

    private static ApiResponse MapExceptionToResponse(Exception exception)
    {
        var code = exception switch
        {
            TimeoutException or TaskCanceledException => ResponseCode.HTTPTimeout,
            HttpRequestException httpException when httpException.StatusCode == HttpStatusCode.TooManyRequests || httpException.Message.Contains(((int)HttpStatusCode.TooManyRequests).ToString(), StringComparison.OrdinalIgnoreCase) => ResponseCode.HttpRE429,
            HttpRequestException httpException when httpException.StatusCode == HttpStatusCode.Unauthorized || httpException.Message.Contains(((int)HttpStatusCode.Unauthorized).ToString(), StringComparison.OrdinalIgnoreCase) => ResponseCode.HttpRE401,
            _ => ResponseCode.GeneralExceptionError
        };

        return new ApiResponse
        {
            IsSuccess = false,
            StatusCode = code,
            ErrorMessage = exception.Message
        };
    }

    private ApiResponse MapResponse(RestResponse response)
    {
        if (response.ContentLength > _maxResponseSizeBytes || (response.RawBytes != null && response.RawBytes.Length > _maxResponseSizeBytes) || (response.Content != null && response.Content.Length > _maxResponseSizeBytes))
        {
            LogResponsePayloadTooLarge(response.ResponseUri?.ToString() ?? "Unknown", response.ContentLength ?? response.RawBytes?.Length ?? response.Content?.Length ?? 0);

            return new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResponseCode.Error,
                ErrorMessage = ResponsePayloadTooLargeErrorMessage
            };
        }

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
            LogRequestTimedOut(response.Request.Resource);

            return new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResponseCode.HTTPTimeout,
                ErrorMessage = RequestTimeoutErrorMessage
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

        LogApiErrorResponse(response.StatusCode, response.ErrorMessage);

        return new ApiResponse
        {
            IsSuccess = false,
            StatusCode = errorCode,
            ErrorMessage = response.ErrorMessage ?? response.StatusDescription
        };
    }

    private static TokenBucketRateLimiter CreateDefaultRateLimiter() =>
        new(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 30,
            TokensPerPeriod = 30,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 100,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

    public void Dispose()
    {
        if (_ownsRateLimiter)
        {
            _rateLimiter.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_ownsRateLimiter)
        {
            await _rateLimiter.DisposeAsync().ConfigureAwait(false);
        }
    }


    [LoggerMessage(
        EventId = LogEventIds.Api.ApiRequestFailed,
        Level = LogLevel.Error,
        Message = "Error during synchronous API invocation: {Url}")]
    private partial void LogSynchronousInvocationFailed(Exception exception, string url);

    [LoggerMessage(
        EventId = LogEventIds.Api.ApiAsyncRequestFailed,
        Level = LogLevel.Error,
        Message = "Error during asynchronous API invocation: {Url}")]
    private partial void LogAsynchronousInvocationFailed(Exception exception, string url);

    [LoggerMessage(
        EventId = LogEventIds.Api.ApiRateLimited,
        Level = LogLevel.Warning,
        Message = "Outbound API rate limit exceeded or queue full for {Url}")]
    private partial void LogRateLimitExceeded(string url);

    [LoggerMessage(
        EventId = LogEventIds.Api.ApiResponsePayloadTooLarge,
        Level = LogLevel.Warning,
        Message = "Response payload for {Url} exceeds allowed limit: {Bytes} bytes")]
    private partial void LogResponsePayloadTooLarge(string url, long bytes);

    [LoggerMessage(
        EventId = LogEventIds.Api.ApiRequestTimedOut,
        Level = LogLevel.Warning,
        Message = "API error: local timeout at {Resource}")]
    private partial void LogRequestTimedOut(string? resource);

    [LoggerMessage(
        EventId = LogEventIds.Api.ApiErrorResponse,
        Level = LogLevel.Warning,
        Message = "API error: {StatusCode} - {ErrorMessage}")]
    private partial void LogApiErrorResponse(HttpStatusCode statusCode, string? errorMessage);
}
