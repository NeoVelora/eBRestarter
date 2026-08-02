using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.ObjectArchetypes.Models;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS;

public sealed class AdapterWindowsBrowserDiscoveryProvider : IOutboundPortBrowserDiscoveryProvider
{
    private const string BrowserTypeNotSupportedLogMessage = "Browser type {Type} is currently not supported by the factory implementation.";
    private const string ErrorEvaluatingBrowserDetailsLogMessage = "An error occurred while evaluating details for browser type {Type}";
    private const string NotInstalledStatusText = "Not installed";

    private readonly IOutboundPortBrowserFactory _browserFactory;
    private readonly ILogger<AdapterWindowsBrowserDiscoveryProvider> _logger;


    public AdapterWindowsBrowserDiscoveryProvider(
        IOutboundPortBrowserFactory browserFactory,
        ILogger<AdapterWindowsBrowserDiscoveryProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(browserFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _browserFactory = browserFactory;
        _logger = logger;
    }


    public Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync()
    {
        var browsers = new List<BrowserInfo>();

        foreach (BrowserType type in Enum.GetValues<BrowserType>())
        {
            try
            {
                IOutboundPortBrowser browserLogic = _browserFactory.Create(type);

                bool isInstalled = browserLogic.IsInstalled;
                string version = isInstalled ? browserLogic.BrowserVersion : NotInstalledStatusText;
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
                    DownloadUrl = downloadUrl
                };

                browsers.Add(browserInfo);
            }
            catch (NotSupportedException exception)
            {
                _logger.LogWarning(exception, BrowserTypeNotSupportedLogMessage, type);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, ErrorEvaluatingBrowserDetailsLogMessage, type);
            }
        }

        return Task.FromResult<IEnumerable<BrowserInfo>>(browsers);
    }
}
