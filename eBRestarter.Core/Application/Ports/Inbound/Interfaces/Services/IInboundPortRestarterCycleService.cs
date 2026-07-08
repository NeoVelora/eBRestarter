using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Services;

public interface IInboundPortRestarterCycleService
{
    event EventHandler<RestarterCycleProgress> ProgressChanged;

    Task StartAsync(ManageRestarterCycleRequest request, Func<Task> performCleanupCallback);

    void Stop();
}

