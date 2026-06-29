using eBRestarter.Core.Application.Models;

namespace eBRestarter.Core.Application.Ports.Outbound.Browser;

public interface IBrowserDiscoveryPort
{
    Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync();
}

