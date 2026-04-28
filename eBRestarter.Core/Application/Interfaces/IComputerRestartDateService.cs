namespace eBRestarter.Core.Application.Interfaces;

public interface IComputerRestartDateService
{
    DateTime GetNextRestartDate(int intervalDays, int restartClockTime);
}
