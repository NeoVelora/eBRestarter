namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Services;

public interface IInboundPortComputerRestartService
{
    event EventHandler<DateTime?>? NextRestartDateChanged;

    void StartScheduler();

    Task StopSchedulerAsync();
}