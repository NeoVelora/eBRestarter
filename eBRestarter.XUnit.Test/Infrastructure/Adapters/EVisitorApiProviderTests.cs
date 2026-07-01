using eBRestarter.Infrastructure.Adapters;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Infrastructure.Adapters.RestSharp;
using eBRestarter.Infrastructure.Network;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Adapters.RestSharp;
using eBRestarter.Infrastructure.Network;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Network;
using eBRestarter.Core.Domain.Entities;
using eBRestarter.Infrastructure.Api;
using eBRestarter.Infrastructure.Browser;
using eBRestarter.Infrastructure.OperatingSystem;
using eBRestarter.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Adapters
{
    public class EVisitorApiProviderTests
    {
        private readonly Mock<IRestClientPort> _mockRestClient;
        private readonly Mock<IEVisitorConfigRepositoryOutboundPort> _mockConfigService;
        private readonly Mock<ILogger<EVisitorApiProvider>> _mockLogger;
        private readonly EVisitorApiProvider _sut;

        public EVisitorApiProviderTests()
        {
            _mockRestClient = new Mock<IRestClientPort>();
            _mockConfigService = new Mock<IEVisitorConfigRepositoryOutboundPort>();
            _mockLogger = new Mock<ILogger<EVisitorApiProvider>>();

            _sut = new EVisitorApiProvider(
                _mockRestClient.Object,
                _mockConfigService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetIpInfoAsync_ShouldParseJsonCorrectly_WhenApiReturnsValidData()
        {
            string json = @"{ ""ip"": ""192.168.1.1"", ""host"": ""test.host.com"", ""countryCode"": ""DE"", ""countryName"": ""Germany"" }";

            _mockRestClient
                .Setup(r => r.ExecuteGetAsync(It.Is<ApiRequest>(req => req.Url == ApiWebLinks.IpLink)))
                .ReturnsAsync(new ApiResponse { IsSuccess = true, Content = json });

            var result = await _sut.RetrieveIpInfoAsync();

            result.ShouldNotBeNull();
            result.IpAddress.ShouldBe("192.168.1.1");
            result.Hostname.ShouldBe("test.host.com");
            result.CountryCode.ShouldBe("DE");
            result.CountryName.ShouldBe("Germany");
        }

        [Fact]
        public async Task GetIpInfoAsync_ShouldReturnNull_OnFailureOrInvalidJson()
        {
            _mockRestClient
                .Setup(r => r.ExecuteGetAsync(It.IsAny<ApiRequest>()))
                .ReturnsAsync(new ApiResponse { IsSuccess = false });

            var resultFail = await _sut.RetrieveIpInfoAsync();

            _mockRestClient
                .Setup(r => r.ExecuteGetAsync(It.IsAny<ApiRequest>()))
                .ReturnsAsync(new ApiResponse { IsSuccess = true, Content = "Kein { gueltiges ] JSON" });

            var resultInvalid = await _sut.RetrieveIpInfoAsync();

            resultFail.ShouldBeNull();
            resultInvalid.ShouldBeNull();
        }

        [Fact]
        public async Task GetEarningsAsync_ShouldReturnNull_WhenCredentialsAreMissing()
        {
            var emptyConfig = new AppConfig();
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(emptyConfig);

            var result = await _sut.RetrieveEarningsAsync();

            result.ShouldBeNull();
            _mockRestClient.Verify(r => r.ExecuteGetAsync(It.IsAny<ApiRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetEarningsAsync_ShouldFetchAndAggregateDataCorrectly()
        {
            var validConfig = new AppConfig();
            validConfig.Settings.ApiUsername = "user";
            validConfig.Settings.ApiKey = "pass";
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(validConfig);

            _mockRestClient
                .Setup(r => r.ExecuteGetAsync(It.IsAny<ApiRequest>()))
                .ReturnsAsync((ApiRequest req) =>
                {
                    if (req.Url == ApiWebLinks.HourlyEarnings)
                    {
                        return new ApiResponse { IsSuccess = true, Content = @"{""1"": 10.5, ""3"": 20.25}" };
                    }

                    string historicalJson = @"[
                        { ""from_w3c"": ""2026-01-05T00:00:00Z"", ""value"": 100.0 },
                        { ""from_w3c"": ""2026-01-05T12:00:00Z"", ""value"": ""50.5"" },
                        { ""from_w3c"": ""2026-02-15T00:00:00Z"", ""value"": 200.0 }
                    ]";

                    return new ApiResponse { IsSuccess = true, Content = historicalJson };
                });

            var result = await _sut.RetrieveEarningsAsync();

            result.ShouldNotBeNull();
            result.HourlyEarnings[0].ShouldBe(10.5);
            result.HourlyEarnings[2].ShouldBe(20.25);
            result.TodaySum.ShouldBe(30.75);
            result.DailyEarnings[4].ShouldBe(150.5);
            result.DailyEarnings[14].ShouldBe(200.0);

            result.MonthlySum.ShouldBe(350.5);
            result.MonthlyEarnings[0].ShouldBe(150.5);
            result.MonthlyEarnings[1].ShouldBe(200.0);

            result.YearlySum.ShouldBe(350.5);
        }

        [Fact]
        public async Task GetEarningsAsync_ShouldHandleHourlyArrayFallback_Correctly()
        {
            var validConfig = new AppConfig();
            validConfig.Settings.ApiUsername = "u";
            validConfig.Settings.ApiKey = "p";
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(validConfig);

            _mockRestClient
                .Setup(r => r.ExecuteGetAsync(It.IsAny<ApiRequest>()))
                .ReturnsAsync((ApiRequest req) =>
                {
                    if (req.Url == ApiWebLinks.HourlyEarnings)
                    {
                        return new ApiResponse { IsSuccess = true, Content = @"[15.0, 5.0]" };
                    }
                    return new ApiResponse { IsSuccess = true, Content = "[]" };
                });

            var result = await _sut.RetrieveEarningsAsync();

            result.ShouldNotBeNull();
            result.HourlyEarnings[0].ShouldBe(15.0);
            result.HourlyEarnings[1].ShouldBe(5.0);
            result.TodaySum.ShouldBe(20.0);
        }
    }
}


