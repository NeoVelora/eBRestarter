using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

public interface IUseCaseToggleEdgeStartupBoost
{
    bool IsEnabled();
    bool IsEdgeInstalled();
    ToggleEdgeStartupBoostResponse Toggle(bool shouldEnable);
}
