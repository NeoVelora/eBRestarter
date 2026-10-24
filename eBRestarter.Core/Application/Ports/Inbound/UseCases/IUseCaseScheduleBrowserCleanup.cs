using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

public interface IUseCaseScheduleBrowserCleanup
{
    ScheduleBrowserCleanupResponse UpdateSchedule(ScheduleBrowserCleanupRequest request);
}

