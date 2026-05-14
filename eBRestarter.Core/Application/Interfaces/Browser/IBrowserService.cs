using eBRestarter.Core.Application.Models;

namespace eBRestarter.Core.Application.Interfaces.Browser;

public interface IBrowserService
{
    Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync();
}
