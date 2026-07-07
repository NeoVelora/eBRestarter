using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

public interface IOutboundPortBrowserFactory
{
    IOutboundPortBrowser Create(BrowserType type);
}


