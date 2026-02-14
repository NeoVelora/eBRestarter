using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Domain.Models.Records.Config;
using System;

namespace eBRestarter.Core.Application.Services
{
    public class BrowserCleanupScheduleService : IBrowserCleanupScheduleService
    {
        #region PublicAndProtectedMethods

        public bool ShouldRunCleanupNow(AppConfig config)
        {
            if (config?.Browser == null || config.Browser.DeleteBrowserCacheIntervalDays <= 0)
                return false;

            return DateTime.Today >= config.Browser.NextBrowserDeleteCacheDate;
        }

        public DateTime GetNextCleanupDateAfterRun(DateTime fromDate, int intervalDays)
        {
            return fromDate.AddDays(intervalDays);
        }

        #endregion
    }
}
