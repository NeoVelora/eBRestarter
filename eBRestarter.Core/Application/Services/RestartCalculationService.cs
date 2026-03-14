using System;
using eBRestarter.Core.Application.Interfaces;

namespace eBRestarter.Core.Application.Services;

public class RestartCalculationService : IRestartCalculationService
{
    private readonly TimeProvider _timeProvider;

    public RestartCalculationService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    #region PublicAndProtectedMethods

    public DateTime GetNextRestartDate(int intervalDays, int restartClockTime)
    {
        if (intervalDays > 0)
        {
            return _timeProvider.GetLocalNow().Date.AddDays(intervalDays).AddHours(restartClockTime);
        }

        return DateTime.MinValue;
    }

    #endregion
}
