using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseToggleEdgeStartupBoost
{
    bool IsEnabled();
    bool IsEdgeInstalled();
    ToggleEdgeStartupBoostResponse Toggle(bool shouldEnable);
}
