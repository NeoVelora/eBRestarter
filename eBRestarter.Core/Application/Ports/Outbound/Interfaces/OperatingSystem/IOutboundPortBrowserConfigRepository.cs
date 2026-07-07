namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortBrowserConfigRepository
{
    void SetBrowserStartupBoost(bool enable);
    bool IsBrowserStartupBoostEnabled();
}
