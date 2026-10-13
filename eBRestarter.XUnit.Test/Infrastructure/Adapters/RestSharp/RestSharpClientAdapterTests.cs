using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.API;
using eBRestarter.Infrastructure.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.ObjectArchetypes.Model;
using Microsoft.Extensions.Logging.Testing;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Http;
using System.Linq;
using Shouldly;
using System;
using System.Threading.RateLimiting;

namespace eBRestarter.Tests.Infrastructure.Services
{
    public class RestSharpClientAdapterTests : IDisposable
    {
        private readonly FakeLogger<RestSharpClient> _loggerMock;
        private readonly RestSharpClient _sut;
        private readonly WireMockServer _server;

        public RestSharpClientAdapterTests()
        {
            _loggerMock = new FakeLogger<RestSharpClient>();
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());

            _sut = new RestSharpClient(_loggerMock, httpClientFactory);

            _server = WireMockServer.Start();
        }

        [Fact]
        public async Task ExecuteGetAsync_Success_ReturnsSuccessAndContent()
        {
            _server.Given(Request.Create().WithPath("/test").UsingGet())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithBody("Test Content"));

            var requestModel = new ApiRequest
            {
                Url = $"{_server.Urls[0]}/test",
                TimeoutSeconds = 5
            };

            var result = await _sut.ExecuteGetAsync(requestModel);

            Assert.True(result.IsSuccess);
            Assert.Equal(ResponseCode.Success, result.StatusCode);
            Assert.Equal("Test Content", result.Content);
        }

        [Fact]
        public async Task ExecuteGetAsync_WithRateLimitWarning_ReturnsRequestLimitCode()
        {
            _server.Given(Request.Create().WithPath("/ratelimit").UsingGet())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithHeader("X-Ratelimit-Remaining", "5")
                       .WithBody("OK"));

            var requestModel = new ApiRequest { Url = $"{_server.Urls[0]}/ratelimit", TimeoutSeconds = 5 };

            var result = await _sut.ExecuteGetAsync(requestModel);

            Assert.True(result.IsSuccess);
            Assert.Equal(ResponseCode.RequestLimit, result.StatusCode);
        }

        [Fact]
        public async Task ExecuteGetAsync_Http401_ReturnsMappedErrorCode()
        {
            _server.Given(Request.Create().WithPath("/unauthorized").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(401));

            var requestModel = new ApiRequest { Url = $"{_server.Urls[0]}/unauthorized", TimeoutSeconds = 5 };

            var result = await _sut.ExecuteGetAsync(requestModel);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResponseCode.HttpRE401, result.StatusCode);

            VerifyLoggerWarningWasCalled();
        }

        [Fact]
        public async Task ExecuteGetAsync_WithBasicAuth_SendsCredentials()
        {
            _server.Given(
                Request.Create()
                    .WithPath("/auth")
                    .UsingGet()
                    .WithHeader("Authorization", "Basic dXNlcjpwYXNz")
            ).RespondWith(Response.Create().WithStatusCode(200));

            var requestModel = new ApiRequest
            {
                Url = $"{_server.Urls[0]}/auth",
                TimeoutSeconds = 5,
                Username = "user",
                Password = "pass"
            };

            var result = await _sut.ExecuteGetAsync(requestModel);

            Assert.True(result.IsSuccess, "The request failed, indicating that the Authorization header was missing or improperly formatted.");
        }

        [Fact]
        public async Task ExecuteGetAsync_Timeout_ReturnsMappedTimeoutCode()
        {
            _server.Given(Request.Create().WithPath("/timeout").UsingGet())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithDelay(TimeSpan.FromSeconds(6)));

            var requestModel = new ApiRequest { Url = $"{_server.Urls[0]}/timeout", TimeoutSeconds = 1 };

            var result = await _sut.ExecuteGetAsync(requestModel);

            Assert.False(result.IsSuccess);
            Assert.True(result.StatusCode == ResponseCode.HTTPTimeout || result.StatusCode == ResponseCode.GeneralExceptionError);
        }

        [Fact]
        public async Task ExecuteGetAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var requestModel = new ApiRequest
            {
                Url = $"{_server.Urls[0]}/cancelled-test",
                TimeoutSeconds = 5
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _sut.ExecuteGetAsync(requestModel, cts.Token));
        }

        [Fact]
        public async Task ExecuteGetAsync_WhenTokenCancelledDuringDelay_ThrowsOperationCanceledException()
        {
            _server.Given(Request.Create().WithPath("/slow-cancel").UsingGet())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithBody("Slow Content")
                       .WithDelay(TimeSpan.FromSeconds(5)));

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(200));

            var requestModel = new ApiRequest
            {
                Url = $"{_server.Urls[0]}/slow-cancel",
                TimeoutSeconds = 10
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _sut.ExecuteGetAsync(requestModel, cts.Token));
        }

        [Fact]
        public void ExecuteGet_SyncCall_Success_ReturnsSuccessAndContent()
        {
            _server.Given(Request.Create().WithPath("/sync-test").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(200).WithBody("Sync Content"));

            var requestModel = new ApiRequest { Url = $"{_server.Urls[0]}/sync-test", TimeoutSeconds = 5 };

            var result = _sut.ExecuteGet(requestModel);

            Assert.True(result.IsSuccess);
            Assert.Equal(ResponseCode.Success, result.StatusCode);
            Assert.Equal("Sync Content", result.Content);
        }

        [Fact]
        public async Task ExecuteGetAsync_BurstExceedingRateLimit_ReturnsRequestLimitCodeAndRecovers()
        {
            _server.Given(Request.Create().WithPath("/rate-test").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(200).WithBody("Rate Content"));

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());

            using var strictLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2,
                TokensPerPeriod = 2,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(400),
                QueueLimit = 0,
                AutoReplenishment = true
            });

            using var clientWithLimiter = new RestSharpClient(_loggerMock, httpClientFactory, strictLimiter);
            var request = new ApiRequest { Url = $"{_server.Urls[0]}/rate-test", TimeoutSeconds = 5 };

            var firstResult = await clientWithLimiter.ExecuteGetAsync(request, TestContext.Current.CancellationToken);
            var secondResult = await clientWithLimiter.ExecuteGetAsync(request, TestContext.Current.CancellationToken);

            var rateLimitedResult = await clientWithLimiter.ExecuteGetAsync(request, TestContext.Current.CancellationToken);

            firstResult.IsSuccess.ShouldBeTrue();
            secondResult.IsSuccess.ShouldBeTrue();
            rateLimitedResult.IsSuccess.ShouldBeFalse();
            rateLimitedResult.StatusCode.ShouldBe(ResponseCode.RequestLimit);

            await Task.Delay(450, TestContext.Current.CancellationToken);
            var recoveredResult = await clientWithLimiter.ExecuteGetAsync(request, TestContext.Current.CancellationToken);

            recoveredResult.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void ExecuteGet_SyncCall_BurstExceedingRateLimit_ReturnsRequestLimitCode()
        {
            _server.Given(Request.Create().WithPath("/sync-rate-test").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(200).WithBody("Sync Content"));

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());

            using var strictLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(500),
                QueueLimit = 0,
                AutoReplenishment = true
            });

            using var clientWithLimiter = new RestSharpClient(_loggerMock, httpClientFactory, strictLimiter);
            var request = new ApiRequest { Url = $"{_server.Urls[0]}/sync-rate-test", TimeoutSeconds = 5 };

            var firstResult = clientWithLimiter.ExecuteGet(request);

            var secondResult = clientWithLimiter.ExecuteGet(request);

            firstResult.IsSuccess.ShouldBeTrue();
            secondResult.IsSuccess.ShouldBeFalse();
            secondResult.StatusCode.ShouldBe(ResponseCode.RequestLimit);
        }

        [Fact]
        public async Task ExecuteGetAsync_ResponseExceedingMaxAllowedSize_ReturnsError()
        {
            _server.Given(Request.Create().WithPath("/huge-payload").UsingGet())
                   .RespondWith(Response.Create()
                       .WithStatusCode(200)
                       .WithBody(new string('X', 100)));

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());

            using var clientWithStrictSize = new RestSharpClient(
                _loggerMock,
                httpClientFactory,
                rateLimiter: null,
                maxResponseSizeBytes: 50);

            var requestModel = new ApiRequest
            {
                Url = $"{_server.Urls[0]}/huge-payload",
                TimeoutSeconds = 5
            };

            var result = await clientWithStrictSize.ExecuteGetAsync(requestModel, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
            result.StatusCode.ShouldBe(ResponseCode.Error);
        }

        [Fact]
        public async Task ExecuteGetAsync_WhenExceptionOccurs_ReturnsSanitizedErrorMessageWithoutLeakingExceptionDetails()
        {
            var requestModel = new ApiRequest
            {
                Url = "http://invalid uri with spaces:unreachable",
                TimeoutSeconds = 1
            };

            var result = await _sut.ExecuteGetAsync(requestModel, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
            result.ErrorMessage.ShouldNotContain("invalid uri with spaces");
            result.ErrorMessage.ShouldBe("An error occurred while executing the HTTP request.");

            var loggedErrors = _loggerMock.Collector.GetSnapshot()
                .Where(e => e.Level == LogLevel.Error)
                .ToList();
            loggedErrors.ShouldNotBeEmpty();
            loggedErrors[0].Exception.ShouldNotBeNull();
        }

        private void VerifyLoggerWarningWasCalled()
        {
            var matchingWarnings = _loggerMock.Collector.GetSnapshot()
                .Where(entry => entry.Level == LogLevel.Warning
                                && entry.Message.Contains("api error", StringComparison.OrdinalIgnoreCase))
                .ToList();

            matchingWarnings.ShouldNotBeEmpty();
        }

        public void Dispose()
        {
            _server.Stop();
            _server.Dispose();
        }

        [Fact]
        public async Task ExecuteGetAsync_WhenPollyTimeoutRejectedExceptionOccurs_ReturnsHttpTimeoutStatus()
        {
            var customHttpClientFactory = Substitute.For<IHttpClientFactory>();
            var throwingClient = new HttpClient(new ThrowingHttpMessageHandler(new TimeoutRejectedException("Polly timeout triggered")));
            customHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(throwingClient);

            var sut = new RestSharpClient(_loggerMock, customHttpClientFactory);
            var request = new ApiRequest
            {
                Url = "http://localhost:5000/api/test",
                TimeoutSeconds = 5
            };

            var result = await sut.ExecuteGetAsync(request, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
            result.StatusCode.ShouldBe(ResponseCode.HTTPTimeout);
            result.ErrorMessage.ShouldBe("The request has exceeded the time limit");
        }

        [Fact]
        public async Task ExecuteGetAsync_WhenPollyBrokenCircuitExceptionOccurs_ReturnsNoConnectionToServer()
        {
            var customHttpClientFactory = Substitute.For<IHttpClientFactory>();
            var throwingClient = new HttpClient(new ThrowingHttpMessageHandler(new BrokenCircuitException("Circuit is open")));
            customHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(throwingClient);

            var sut = new RestSharpClient(_loggerMock, customHttpClientFactory);
            var request = new ApiRequest
            {
                Url = "http://localhost:5000/api/test",
                TimeoutSeconds = 5
            };

            var result = await sut.ExecuteGetAsync(request, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
            result.StatusCode.ShouldBe(ResponseCode.NoConnectionToServer);
            result.ErrorMessage.ShouldBe("An error occurred while executing the HTTP request.");
        }

        private sealed class ThrowingHttpMessageHandler(Exception exceptionToThrow) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromException<HttpResponseMessage>(exceptionToThrow);
            }
        }
    }
}
