using eBRestarter.Core.Application.Interfaces;
using System;

namespace eBRestarter.Core.Application.Services
{
    public class RestartCalculationService : IRestartCalculationService
    {
        #region PublicAndProtectedMethods

        public DateTime GetNextRestartDate(int intervalDays, int restartClockTime)
        {
            if (intervalDays > 0)
            {
                return DateTime.Today.AddDays(intervalDays).AddHours(restartClockTime);
            }

            return DateTime.MinValue;
        }

        #endregion
    }
}
