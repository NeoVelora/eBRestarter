namespace eBRestarter.Core.Domain.Handlers;

public interface IRestartCalculationHandler
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}