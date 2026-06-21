namespace eBRestarter.Core.Application.Interfaces;

public interface IRetrieveNextRestartDateUseCase
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}
