using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.Core.Application.Ports.Outbound.Browser;

public interface IOutboundPortBrowserFactory
{
    IOutboundPortBrowser Create(BrowserType type);
}
