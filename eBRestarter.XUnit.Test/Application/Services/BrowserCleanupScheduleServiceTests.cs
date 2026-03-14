using eBRestarter.Core.Application.Services;
using eBRestarter.Core.Domain.Models.Records.Config;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Application.Services;

public class BrowserCleanupScheduleServiceTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly BrowserCleanupScheduleService _sut;

    public BrowserCleanupScheduleServiceTests()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 3, 10, 14, 0, 0, TimeSpan.Zero));
        _sut = new BrowserCleanupScheduleService(_fakeTimeProvider);
    }

    [Fact]
    public void ShouldRunCleanupNow_WhenConfigIsNull_ShouldReturnFalse()
    {
        var result = _sut.ShouldRunCleanupNow(null!);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldRunCleanupNow_WhenIntervalIsZero_ShouldReturnFalse()
    {
        var config = new AppConfig();
        config.Browser.DeleteBrowserCacheIntervalDays = 0;
        config.Browser.NextBrowserDeleteCacheDate = new DateTime(2026, 3, 9);

        var result = _sut.ShouldRunCleanupNow(config);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldRunCleanupNow_WhenDateIsPastOrToday_ShouldReturnTrue()
    {
        var config = new AppConfig();
        config.Browser.DeleteBrowserCacheIntervalDays = 1;

        config.Browser.NextBrowserDeleteCacheDate = new DateTime(2026, 3, 10);

        var result = _sut.ShouldRunCleanupNow(config);

        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldRunCleanupNow_WhenDateIsInFuture_ShouldReturnFalse()
    {
        var config = new AppConfig();
        config.Browser.DeleteBrowserCacheIntervalDays = 1;

        config.Browser.NextBrowserDeleteCacheDate = new DateTime(2026, 3, 11);

        var result = _sut.ShouldRunCleanupNow(config);

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetNextCleanupDateAfterRun_ShouldAddIntervalDays()
    {
        var fromDate = new DateTime(2026, 3, 10);
        int intervalDays = 7;

        var result = _sut.GetNextCleanupDateAfterRun(fromDate, intervalDays);

        result.ShouldBe(new DateTime(2026, 3, 17));
    }
}