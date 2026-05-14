namespace eBRestarter.Core.Application.Interfaces;

public interface IComputerRestartDateService
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}
