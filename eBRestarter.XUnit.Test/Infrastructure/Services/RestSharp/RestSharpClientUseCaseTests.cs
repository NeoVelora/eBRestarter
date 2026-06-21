using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Api;
using eBRestarter.Infrastructure.Services.RestSharp;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services
{
    public class RestSharpClientUseCaseTests : IDisposable
    {
        private readonly Mock<ILogger<RestSharpClientHandler>> _loggerMock;
        private readonly RestSharpClientHandler _sut;
        private readonly WireMockServer _server;

        public RestSharpClientUseCaseTests()
        {
            _loggerMock = new Mock<ILogger<RestSharpClientHandler>>();
            _sut = new RestSharpClientHandler(_loggerMock.Object);

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

            Assert.True(result.IsSuccess, "Die Anfrage ist fehlgeschlagen. Das bedeutet, dass der Auth-Header nicht oder falsch gesendet wurde.");
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
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API Fehler")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        public void Dispose()
        {
            _server.Stop();
            _server.Dispose();
        }
    }
}

