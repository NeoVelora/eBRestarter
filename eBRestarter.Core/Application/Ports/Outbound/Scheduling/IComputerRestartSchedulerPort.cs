namespace eBRestarter.Core.Application.Ports.Outbound.Scheduling;

public interface IComputerRestartSchedulerPort
{
    event EventHandler<DateTime?>? OnNextRestartDateChanged;

    void StartScheduler();

    Task StopSchedulerAsync();
}