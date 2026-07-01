using eBRestarter.Core.Application.Models;

namespace eBRestarter.Core.Application.Ports.Outbound.Browser;

public interface IBrowserDiscoveryProviderOutboundPort
{
    Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync();
}

