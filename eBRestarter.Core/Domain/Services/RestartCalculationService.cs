namespace eBRestarter.Core.Domain.Services;

/// <summary>
/// Domänenservice zur zonenbewussten Berechnung von Neustartzeiten.
/// </summary>
public sealed class RestartCalculationService(TimeProvider timeProvider) : IRestartCalculationService
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime)
    {
        if (intervalDays > 0)
        {
            return _timeProvider.GetLocalNow().Date.AddDays(intervalDays).AddHours(restartClockTime);
        }

        return DateTime.MinValue;
    }
}

