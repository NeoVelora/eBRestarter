using eBRestarter.Core.Application.ObjectArchetypes.Models;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

public interface IOutboundPortBrowserDiscoveryProvider
{
    Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync();
}

