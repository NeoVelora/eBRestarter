namespace eBRestarter.Core.Domain.Services;

public class RestartCalculationHandler(TimeProvider timeProvider) : IRestartCalculationHandler
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
