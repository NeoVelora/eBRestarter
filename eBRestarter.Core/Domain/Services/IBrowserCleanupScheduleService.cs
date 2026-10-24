namespace eBRestarter.Core.Domain.Services;

/// <summary>
/// Definiert Planungsoperationen für Browser-Bereinigungen in der Domäne.
/// </summary>
public interface IBrowserCleanupScheduleService
{
    bool ShouldRunCleanupNow(int deleteBrowserCacheIntervalDays, DateTime nextBrowserDeleteCacheDate);
    DateTime CalculateNextCleanupDateAfterRun(DateTime fromDate, int intervalDays);
}

