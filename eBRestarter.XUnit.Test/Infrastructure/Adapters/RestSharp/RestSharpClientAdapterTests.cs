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
using System.Net.Http;
using System.Linq;
using Shouldly;
using System;

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
    }
}
