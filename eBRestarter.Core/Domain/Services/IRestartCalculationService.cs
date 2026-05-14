namespace eBRestarter.Core.Domain.Services;

public interface IRestartCalculationService
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}
