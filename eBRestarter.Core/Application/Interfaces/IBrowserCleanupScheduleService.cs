using eBRestarter.Core.Domain.Models.Records.Config;

namespace eBRestarter.Core.Application.Interfaces;

public interface IBrowserCleanupScheduleService
{
    #region PublicMethods

    bool ShouldRunCleanupNow(AppConfig config);

    DateTime GetNextCleanupDateAfterRun(DateTime fromDate, int intervalDays);

    #endregion
}
