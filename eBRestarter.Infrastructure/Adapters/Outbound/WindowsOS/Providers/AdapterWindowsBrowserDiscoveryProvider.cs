using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.ObjectArchetypes.Models;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Providers;

public sealed class AdapterWindowsBrowserDiscoveryProvider(IOutboundPortBrowserFactory browserFactoryPort, ILogger<AdapterWindowsBrowserDiscoveryProvider> logger) : IOutboundPortBrowserDiscoveryProvider
{
    private readonly IOutboundPortBrowserFactory _browserFactory = browserFactoryPort;
    private readonly ILogger<AdapterWindowsBrowserDiscoveryProvider> _logger = logger;

    public async Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync()
    {
        var browsers = new List<BrowserInfo>();

        foreach (BrowserType type in Enum.GetValues<BrowserType>())
        {
            try
            {
                IOutboundPortBrowser browserLogic = _browserFactory.Create(type);

                bool isInstalled = browserLogic.IsInstalled;
                string version = isInstalled ? browserLogic.BrowserVersion : "Not installed";
                string displayName = browserLogic.DisplayName;
                string iconPath = browserLogic.IconPath;
                string downloadUrl = browserLogic.DownloadUrl;

                var browserInfo = new BrowserInfo
                {
                    Type = type,
                    Name = displayName,
                    IsInstalled = isInstalled,
                    Version = version,
                    IconPath = iconPath,
                    DownloadUrl = downloadUrl,
                };

                browsers.Add(browserInfo);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "Browser type {Type} is currently not supported by the factory implementation.", type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while evaluating details for browser type {Type}", type);
            }
        }

        return await Task.FromResult(browsers);
    }
}


