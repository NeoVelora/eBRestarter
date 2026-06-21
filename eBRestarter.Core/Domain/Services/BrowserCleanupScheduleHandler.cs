namespace eBRestarter.Core.Domain.Services;

public class BrowserCleanupScheduleHandler(TimeProvider timeProvider) : IBrowserCleanupScheduleHandler
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public bool ShouldRunCleanupNow(int deleteBrowserCacheIntervalDays, DateTime nextBrowserDeleteCacheDate)
    {
        if (deleteBrowserCacheIntervalDays <= 0) return false;

        return _timeProvider.GetLocalNow().Date >= nextBrowserDeleteCacheDate.Date;
    }

    public DateTime CalculateNextCleanupDateAfterRun(DateTime fromDate, int intervalDays)
    {
        return fromDate.AddDays(intervalDays);
    }
}
