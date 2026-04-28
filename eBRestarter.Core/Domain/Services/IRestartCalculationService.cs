namespace eBRestarter.Core.Domain.Services;

public interface IRestartCalculationService
{
    DateTime GetNextRestartDate(int intervalDays, int restartClockTime);
}
