using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Models;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.WindowsOS;

public sealed class WindowsBrowserDiscoveryProvider(IBrowserFactoryOutboundPort browserFactoryPort, ILogger<WindowsBrowserDiscoveryProvider> logger) : IBrowserDiscoveryProviderOutboundPort
{
    private readonly IBrowserFactoryOutboundPort _browserFactory = browserFactoryPort ?? throw new ArgumentNullException(nameof(browserFactoryPort));
    private readonly ILogger<WindowsBrowserDiscoveryProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync()
    {
        var browsers = new List<BrowserInfo>();

        foreach (BrowserType type in Enum.GetValues<BrowserType>())
        {
            try
            {
                IBrowserOutboundPort browserLogic = _browserFactory.Create(type);

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


