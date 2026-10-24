using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Update;

namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

public interface IUseCaseManageApplicationUpdates
{
    Task<CheckUpdateResponse> CheckForUpdatesAsync();
    Task PerformUpdateAsync();
}

