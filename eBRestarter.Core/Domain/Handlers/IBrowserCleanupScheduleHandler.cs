namespace eBRestarter.Core.Domain.Handlers;

public interface IBrowserCleanupScheduleHandler
{
    bool ShouldRunCleanupNow(int deleteBrowserCacheIntervalDays, DateTime nextBrowserDeleteCacheDate);

    DateTime CalculateNextCleanupDateAfterRun(DateTime fromDate, int intervalDays);
}