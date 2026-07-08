using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseScheduleBrowserCleanup
{
    ScheduleBrowserCleanupResponse UpdateSchedule(ScheduleBrowserCleanupRequest request);
}

