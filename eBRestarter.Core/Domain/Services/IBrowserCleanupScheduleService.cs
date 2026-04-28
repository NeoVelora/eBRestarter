namespace eBRestarter.Core.Domain.Services;

public interface IBrowserCleanupScheduleService
{
    bool ShouldRunCleanupNow(int deleteBrowserCacheIntervalDays, DateTime nextBrowserDeleteCacheDate);

    DateTime GetNextCleanupDateAfterRun(DateTime fromDate, int intervalDays);
}
