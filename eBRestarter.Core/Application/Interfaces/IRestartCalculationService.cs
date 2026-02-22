namespace eBRestarter.Core.Application.Interfaces;

public interface IRestartCalculationService
{
    #region PublicMethods

    DateTime GetNextRestartDate(int intervalDays, int restartClockTime);

    #endregion
}
