namespace eBRestarter.Core.Domain.Services;

public interface IRestartCalculationHandler
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}
