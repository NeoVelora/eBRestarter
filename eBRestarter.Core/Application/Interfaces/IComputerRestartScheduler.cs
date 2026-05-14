namespace eBRestarter.Core.Application.Interfaces;

public interface IComputerRestartScheduler
{
    event EventHandler<DateTime?>? OnNextRestartDateChanged;

    void StartScheduler();

    Task StopSchedulerAsync();


}
