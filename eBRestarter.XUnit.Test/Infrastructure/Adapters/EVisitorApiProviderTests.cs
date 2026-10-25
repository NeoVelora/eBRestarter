using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.API;

using eBRestarter.Infrastructure.BehavioralComponents.Clients.Http;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.ObjectArchetypes.Models;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.Tests.Infrastructure.Adapters
{
    public class EVisitorApiProviderTests
    {
        private readonly IRestClient _mockRestClient;
        private readonly IOutboundPortEVisitorConfigRepository _mockConfigService;
        private readonly FakeLogger<AdapterEVisitorApiProvider> _mockLogger;
        private readonly AdapterEVisitorApiProvider _sut;

        public EVisitorApiProviderTests()
        {
            _mockRestClient = Substitute.For<IRestClient>();
            _mockConfigService = Substitute.For<IOutboundPortEVisitorConfigRepository>();
            _mockLogger = new FakeLogger<AdapterEVisitorApiProvider>();

            _sut = new AdapterEVisitorApiProvider(
                _mockConfigService,
                _mockLogger,
                _mockRestClient);
        }

        [Fact]
        public async Task GetIpInfoAsync_ShouldParseJsonCorrectly_WhenApiReturnsValidData()
        {
            string json = @"{ ""ip"": ""192.168.1.1"", ""host"": ""test.host.com"", ""countryCode"": ""DE"", ""countryName"": ""Germany"" }";

            _mockRestClient
                .ExecuteGetAsync(Arg.Is<ApiRequest>(req => req.Url == ApiWebLinks.IpLink))
                .Returns(new ApiResponse { IsSuccess = true, Content = json });

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
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(new ApiResponse { IsSuccess = false });

            var resultFail = await _sut.RetrieveIpInfoAsync();

            _mockRestClient
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(new ApiResponse { IsSuccess = true, Content = "Invalid { JSON [" });

            var resultInvalid = await _sut.RetrieveIpInfoAsync();

            resultFail.ShouldBeNull();
            resultInvalid.ShouldBeNull();
        }

        [Fact]
        public async Task GetEarningsAsync_ShouldReturnNull_WhenCredentialsAreMissing()
        {
            var emptyConfig = new AppConfig();
            _mockConfigService.LoadConfig().Returns(emptyConfig);

            var result = await _sut.RetrieveEarningsAsync();

            result.ShouldBeNull();
            await _mockRestClient.DidNotReceive().ExecuteGetAsync(Arg.Any<ApiRequest>());
        }

        [Fact]
        public async Task GetEarningsAsync_ShouldFetchAndAggregateDataCorrectly()
        {
            var validConfig = new AppConfig();
            validConfig.Settings.ApiUsername = "user";
            validConfig.Settings.ApiKey = "pass";
            _mockConfigService.LoadConfig().Returns(validConfig);

            _mockRestClient
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(ci => {
                    if (ci.Arg<ApiRequest>().Url == ApiWebLinks.HourlyEarnings)
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
            _mockConfigService.LoadConfig().Returns(validConfig);

            _mockRestClient
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(ci => {
                    if (ci.Arg<ApiRequest>().Url == ApiWebLinks.HourlyEarnings)
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

        [Fact]
        public async Task GetEarningsAsync_ShouldUseInjectedTimeProvider_WhenProvided()
        {
            var fixedDate = new DateTimeOffset(2026, 7, 15, 14, 30, 0, TimeSpan.Zero);
            var fakeTime = new FakeTimeProvider(fixedDate);

            var sut = new AdapterEVisitorApiProvider(
                _mockConfigService,
                _mockLogger,
                _mockRestClient,
                fakeTime);

            var validConfig = new AppConfig();
            validConfig.Settings.ApiUsername = "testuser";
            validConfig.Settings.ApiKey = "testkey";
            _mockConfigService.LoadConfig().Returns(validConfig);

            string expectedHourlyUrl = ApiWebLinks.GetHourlyEarnings(fakeTime);

            _mockRestClient
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(ci => {
                    if (ci.Arg<ApiRequest>().Url == expectedHourlyUrl)
                    {
                        return new ApiResponse { IsSuccess = true, Content = @"[25.0]" };
                    }
                    return new ApiResponse { IsSuccess = true, Content = "[]" };
                });

            var result = await sut.RetrieveEarningsAsync();

            result.ShouldNotBeNull();
            result.HourlyEarnings[0].ShouldBe(25.0);
            await _mockRestClient.Received().ExecuteGetAsync(Arg.Is<ApiRequest>(req => req.Url == expectedHourlyUrl));
        }
    }
}
