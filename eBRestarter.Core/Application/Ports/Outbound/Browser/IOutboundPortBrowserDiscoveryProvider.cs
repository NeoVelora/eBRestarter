using eBRestarter.Core.Application.ObjectArchetypes.Models;

namespace eBRestarter.Core.Application.Ports.Outbound.Browser;

public interface IOutboundPortBrowserDiscoveryProvider
{
    Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync();
}

