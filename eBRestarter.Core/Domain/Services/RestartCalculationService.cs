namespace eBRestarter.Core.Domain.Services;

public class RestartCalculationService(TimeProvider timeProvider) : IRestartCalculationService
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public DateTime GetNextRestartDate(int intervalDays, int restartClockTime)
    {
        if (intervalDays > 0)
        {
            return _timeProvider.GetLocalNow().Date.AddDays(intervalDays).AddHours(restartClockTime);
        }

        return DateTime.MinValue;
    }
}
