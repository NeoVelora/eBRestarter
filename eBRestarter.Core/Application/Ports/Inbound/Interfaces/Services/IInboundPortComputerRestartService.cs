namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Services;

public interface IInboundPortComputerRestartService
{
    event EventHandler<DateTime?>? OnNextRestartDateChanged;

    void StartScheduler();

    Task StopSchedulerAsync();
}