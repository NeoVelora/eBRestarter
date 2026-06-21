namespace eBRestarter.Core.Domain.Services;

public interface IBrowserCleanupScheduleHandler
{
    bool ShouldRunCleanupNow(int deleteBrowserCacheIntervalDays, DateTime nextBrowserDeleteCacheDate);

    DateTime CalculateNextCleanupDateAfterRun(DateTime fromDate, int intervalDays);
}
