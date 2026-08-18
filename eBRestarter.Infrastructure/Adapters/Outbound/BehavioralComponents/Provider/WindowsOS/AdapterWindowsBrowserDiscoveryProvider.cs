using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.ObjectArchetypes.Models;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS;

public sealed class AdapterWindowsBrowserDiscoveryProvider : IOutboundPortBrowserDiscoveryProvider, IDisposable
{
    private const string BrowserTypeNotSupportedLogMessage = "Browser type {Type} is currently not supported by the factory implementation.";
    private const string ErrorEvaluatingBrowserDetailsLogMessage = "An error occurred while evaluating details for browser type {Type}";
    private const string NotInstalledStatusText = "Not installed";

    private static readonly TimeSpan DiscoveryCacheTimeToLive = TimeSpan.FromSeconds(30);

    private static readonly BrowserType[] AllBrowserTypes = Enum.GetValues<BrowserType>();

    private readonly IOutboundPortBrowserFactory _browserFactory;
    private readonly ILogger<AdapterWindowsBrowserDiscoveryProvider> _logger;
    private readonly TimeProvider _timeProvider;

    private readonly SemaphoreSlim _discoveryGate = new(1, 1);
    private IReadOnlyList<BrowserInfo>? _cachedBrowsers;
    private long _cacheTimestampTicks;
    private bool _disposed;


    public AdapterWindowsBrowserDiscoveryProvider(
        IOutboundPortBrowserFactory browserFactory,
        ILogger<AdapterWindowsBrowserDiscoveryProvider> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(browserFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _browserFactory = browserFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }


    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _discoveryGate.Dispose();
    }

    public async Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync()
    {
        if (TryReadFreshCache(out IReadOnlyList<BrowserInfo>? cached))
        {
            return cached;
        }

        await _discoveryGate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (TryReadFreshCache(out cached))
            {
                return cached;
            }

            IReadOnlyList<BrowserInfo> discovered = DiscoverInstalledBrowsers();

            _cachedBrowsers = discovered;
            Volatile.Write(ref _cacheTimestampTicks, _timeProvider.GetUtcNow().UtcTicks);

            return discovered;
        }
        finally
        {
            _discoveryGate.Release();
        }
    }

    private List<BrowserInfo> DiscoverInstalledBrowsers()
    {
        var browsers = new List<BrowserInfo>(AllBrowserTypes.Length);

        foreach (BrowserType type in AllBrowserTypes)
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
                _logger.LogWarning(LogEventIds.Browser.BrowserTypeNotSupported, exception, BrowserTypeNotSupportedLogMessage, type);
            }
            catch (Exception exception)
            {
                _logger.LogError(LogEventIds.Browser.BrowserDetailEvaluationFailed, exception, ErrorEvaluatingBrowserDetailsLogMessage, type);
            }
        }

        return browsers;
    }

    private bool TryReadFreshCache(out IReadOnlyList<BrowserInfo> cached)
    {
        IReadOnlyList<BrowserInfo>? snapshot = _cachedBrowsers;

        if (snapshot is not null
            && _timeProvider.GetUtcNow().UtcTicks - Volatile.Read(ref _cacheTimestampTicks) < DiscoveryCacheTimeToLive.Ticks)
        {
            cached = snapshot;
            return true;
        }

        cached = [];
        return false;
    }
}
