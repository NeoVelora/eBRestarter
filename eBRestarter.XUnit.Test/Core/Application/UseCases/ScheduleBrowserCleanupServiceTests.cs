using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.UseCases.ScheduleBrowserCleanup;
using eBRestarter.Core.Application.Models.Config;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.Tests.Core.Application.UseCases.ScheduleBrowserCleanup
{
    public class ScheduleBrowserCleanupServiceTests
    {
        private readonly Mock<IEVisitorConfigService> _mockConfigService;
        private readonly FakeTimeProvider _fakeTimeProvider;
        private readonly ScheduleBrowserCleanupService _sut;

        public ScheduleBrowserCleanupServiceTests()
        {
            _mockConfigService = new Mock<IEVisitorConfigService>();
            _fakeTimeProvider = new FakeTimeProvider();

            _sut = new ScheduleBrowserCleanupService(
                _mockConfigService.Object,
                _fakeTimeProvider);
        }


        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(14)]
        public void UpdateSchedule_ShouldActivate_AndCalculateNextDate_WhenIntervalIsValid(int validIntervalDays)
        {
            var testDate = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
            _fakeTimeProvider.SetUtcNow(testDate);

            var dummyConfig = new AppConfig { Browser = new Browser() };
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(dummyConfig);

            AppConfig? savedConfig = null;
            _mockConfigService.Setup(c => c.SaveConfig(It.IsAny<AppConfig>()))
                              .Callback<AppConfig>(config => savedConfig = config);

            var request = new ScheduleBrowserCleanupRequest(validIntervalDays);

            var response = _sut.UpdateSchedule(request);

            response.ShouldNotBeNull();
            response.IsActive.ShouldBeTrue();

            var expectedDate = testDate.Date.AddDays(validIntervalDays);
            response.NextDate.ShouldBe(expectedDate);

            savedConfig.ShouldNotBeNull();
            savedConfig.Browser.DeleteBrowserCacheIntervalDays.ShouldBe(validIntervalDays);
            savedConfig.Browser.NextBrowserDeleteCacheDate.ShouldBe(expectedDate);

            _mockConfigService.Verify(c => c.SaveConfig(It.IsAny<AppConfig>()), Times.Once);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(-1)]
        [InlineData(99)]
        public void UpdateSchedule_ShouldDeactivate_AndClearNextDate_WhenIntervalIsInvalid(int invalidIntervalDays)
        {
            var dummyConfig = new AppConfig { Browser = new Browser() };
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(dummyConfig);

            AppConfig? savedConfig = null;
            _mockConfigService.Setup(c => c.SaveConfig(It.IsAny<AppConfig>()))
                              .Callback<AppConfig>(config => savedConfig = config);

            var request = new ScheduleBrowserCleanupRequest(invalidIntervalDays);

            var response = _sut.UpdateSchedule(request);

            response.ShouldNotBeNull();
            response.IsActive.ShouldBeFalse();
            response.NextDate.ShouldBeNull();

            savedConfig.ShouldNotBeNull();
            savedConfig.Browser.DeleteBrowserCacheIntervalDays.ShouldBe(invalidIntervalDays);
            savedConfig.Browser.NextBrowserDeleteCacheDate.ShouldBe(DateTime.MinValue);

            _mockConfigService.Verify(c => c.SaveConfig(It.IsAny<AppConfig>()), Times.Once);
        }


        [Fact]
        public void UpdateSchedule_ShouldBubbleUpException_WhenConfigFails()
        {
            var request = new ScheduleBrowserCleanupRequest(7);

            _mockConfigService.Setup(c => c.LoadConfig()).Throws(new UnauthorizedAccessException("Access denied"));

            var exception = Should.Throw<UnauthorizedAccessException>(() => _sut.UpdateSchedule(request));
            exception.Message.ShouldBe("Access denied");

            _mockConfigService.Verify(c => c.SaveConfig(It.IsAny<AppConfig>()), Times.Never);
        }
    }
}