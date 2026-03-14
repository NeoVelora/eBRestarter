using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Services;
using eBRestarter.Core.Domain.Models.Records.Config;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Application.Services;

public class RestartTaskDisplayStateServiceTests
{
    private readonly Mock<ILocalizationService> _mockLocalization;
    private readonly Mock<ICacheDeletionIntervalValidator> _mockIntervalValidator;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly RestartTaskDisplayStateService _sut;

    public RestartTaskDisplayStateServiceTests()
    {
        _mockLocalization = new Mock<ILocalizationService>();
        _mockIntervalValidator = new Mock<ICacheDeletionIntervalValidator>();
        _fakeTimeProvider = new FakeTimeProvider();
        
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 3, 10, 14, 0, 0, TimeSpan.Zero));

        _mockLocalization.Setup(l => l.GetString(It.IsAny<string>())).Returns((string key) => key);

        _sut = new RestartTaskDisplayStateService(
            _mockLocalization.Object,
            _mockIntervalValidator.Object,
            _fakeTimeProvider);
    }

    [Fact]
    public void GetInitialState_WhenConfigIsNull_ShouldReturnDefaultState()
    {
        var result = _sut.GetInitialState(null!);

        result.ShouldNotBeNull();
        result.ChoosenBrowser.ShouldBe("Task_DefaultBrowser");
        result.PauseSeconds.ShouldBe(20);
        result.RuntimeSeconds.ShouldBe(3600);
    }

    [Fact]
    public void GetInitialState_WhenDeleteContentIsActive_ShouldFormatMessagesCorrectly()
    {
        var config = new AppConfig();
        config.Username = "testUser";
        config.Browser.Selected = "Edge";
        config.Browser.RuntimeHours = 2;
        config.Browser.RuntimePauseSeconds = 30;
        config.Browser.DeleteBrowserCacheIntervalDays = 3;
        config.Browser.NextBrowserDeleteCacheDate = new DateTime(2026, 3, 15);

        _mockIntervalValidator.Setup(v => v.IsValidIntervalDays(3)).Returns(true);

        _mockLocalization.Setup(l => l.GetString("Browser_NextDeleteDate_Format")).Returns("Next: {0:yyyy-MM-dd}");

        var result = _sut.GetInitialState(config);

        result.Username.ShouldBe("testUser");
        result.ChoosenBrowser.ShouldBe("Edge");
        result.RuntimeSeconds.ShouldBe(7200);
        result.PauseSeconds.ShouldBe(30);
        result.DeleteBrowserContentIsActive.ShouldBeTrue();
        result.DeleteIsActivatedMessage.ShouldBe("Activate");
        result.NextDeletionProcessMessage.ShouldBe("NextDeletionProcess");
        result.NextDeletionProcessDateMessage.ShouldBe("Next: 2026-03-15");
    }

    [Fact]
    public void GetInitialState_WhenDeleteContentIsInactive_ShouldReturnDisabledMessage()
    {
        var config = new AppConfig();
        config.Browser.DeleteBrowserCacheIntervalDays = 0;

        _mockIntervalValidator.Setup(v => v.IsValidIntervalDays(0)).Returns(false);

        var result = _sut.GetInitialState(config);

        result.DeleteBrowserContentIsActive.ShouldBeFalse();
        result.DeleteIsActivatedMessage.ShouldBe("Disabled");
        result.NextDeletionProcessMessage.ShouldBeEmpty();
        result.NextDeletionProcessDateMessage.ShouldBeEmpty();
    }

    [Fact]
    public void GetInitialState_WhenNextDateIsToday_ShouldCalculateNewDateForDisplay()
    {
        var config = new AppConfig();
        config.Browser.DeleteBrowserCacheIntervalDays = 1;
        config.Browser.NextBrowserDeleteCacheDate = new DateTime(2026, 3, 10);

        _mockIntervalValidator.Setup(v => v.IsValidIntervalDays(1)).Returns(true);
        _mockLocalization.Setup(l => l.GetString("Browser_NextDeleteDate_Format")).Returns("{0:yyyy-MM-dd}");

        var result = _sut.GetInitialState(config);

        result.NextDeletionProcessDateMessage.ShouldBe("2026-03-11");
    }
}