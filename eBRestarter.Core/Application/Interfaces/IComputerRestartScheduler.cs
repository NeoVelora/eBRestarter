namespace eBRestarter.Core.Application.Interfaces;

public interface IComputerRestartScheduler
{
    void StartScheduler();

    Task StopSchedulerAsync();
}
