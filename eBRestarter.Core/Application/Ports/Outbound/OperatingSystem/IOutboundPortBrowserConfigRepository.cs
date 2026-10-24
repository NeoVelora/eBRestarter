namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOutboundPortBrowserConfigRepository
{
    void SetBrowserStartupBoost(bool enable);
    bool IsBrowserStartupBoostEnabled();
}
