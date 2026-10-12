using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelInstallAddOn : ObservableObject, IDisposable
{
    private const double BrowserAddonRefreshIntervalMilliseconds = 2000;

    private readonly IOutboundPortBrowserFactory _browserFactory;
    private readonly ILogger<ViewModelInstallAddOn> _logger;

    private readonly DispatcherTimer _timer;

    private volatile bool _disposed;

    private int _refreshInFlight;


    public ObservableCollection<ViewModelBrowserAddonStatus> Browsers { get; } = [];

    public ViewModelInstallAddOn(
        IOutboundPortBrowserFactory browserFactory,
        IInboundPortLocalizationProvider localizationService,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(browserFactory);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _browserFactory = browserFactory;
        _logger = loggerFactory.CreateLogger<ViewModelInstallAddOn>();

        var addonStatusLogger = loggerFactory.CreateLogger<ViewModelBrowserAddonStatus>();

        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Brave), localizationService, addonStatusLogger));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Chrome), localizationService, addonStatusLogger));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Edge), localizationService, addonStatusLogger));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Firefox), localizationService, addonStatusLogger));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Vivaldi), localizationService, addonStatusLogger));

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(BrowserAddonRefreshIntervalMilliseconds)
        };
        _timer.Tick += OnBrowserAddonRefreshTimerTick;
        _timer.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _timer.Stop();
        _timer.Tick -= OnBrowserAddonRefreshTimerTick;
    }

    private async void OnBrowserAddonRefreshTimerTick(object? sender, object eventArgs)
    {
        if (_disposed)
        {
            return;
        }

        if (Interlocked.Exchange(ref _refreshInFlight, 1) == 1)
        {
            return;
        }

        var pendingRefreshes = new List<Task>(Browsers.Count);

        try
        {

            foreach (ViewModelBrowserAddonStatus browserAddonStatus in Browsers)
            {
                pendingRefreshes.Add(browserAddonStatus.RefreshStatusAsync());
            }

            await Task.WhenAll(pendingRefreshes);
        }
        catch (Exception)
        {
            foreach (var failure in pendingRefreshes
                         .Where(task => task.IsFaulted)
                         .SelectMany(task => task.Exception!.Flatten().InnerExceptions))
            {
                LogAddonStatusRefreshFailed(failure);
            }
        }
        finally
        {
            Volatile.Write(ref _refreshInFlight, 0);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.Browser.BrowserProfileDiscoveryFailed,
        Level = LogLevel.Error,
        Message = "Refreshing the add-on status of one browser failed.")]
    private partial void LogAddonStatusRefreshFailed(Exception exception);
}
