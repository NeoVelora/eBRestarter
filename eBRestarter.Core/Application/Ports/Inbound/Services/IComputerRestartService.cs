namespace eBRestarter.Core.Application.Ports.Inbound.Services;

public interface IComputerRestartService
{
    event EventHandler<DateTime?>? OnNextRestartDateChanged;

    void StartScheduler();

    Task StopSchedulerAsync();
}