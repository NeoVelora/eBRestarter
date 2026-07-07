using eBRestarter.Core.Application.Models;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

public interface IOutboundPortBrowserDiscoveryProvider
{
    Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync();
}

