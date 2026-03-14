using eBRestarter.Core.Application.Services;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Application.Services;

public class RestartCalculationServiceTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly RestartCalculationService _sut;

    public RestartCalculationServiceTests()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 3, 10, 14, 0, 0, TimeSpan.Zero));
        _sut = new RestartCalculationService(_fakeTimeProvider);
    }

    [Fact]
    public void GetNextRestartDate_WhenIntervalIsGreaterThanZero_ShouldCalculateDate()
    {
        int intervalDays = 2;
        int restartClockTime = 9;

        var result = _sut.GetNextRestartDate(intervalDays, restartClockTime);

        result.ShouldBe(new DateTime(2026, 3, 12, 9, 0, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetNextRestartDate_WhenIntervalIsZeroOrNegative_ShouldReturnMinValue(int intervalDays)
    {
        int restartClockTime = 9;

        var result = _sut.GetNextRestartDate(intervalDays, restartClockTime);

        result.ShouldBe(DateTime.MinValue);
    }
}