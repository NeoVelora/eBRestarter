using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelInstallAddOn : ObservableObject, IDisposable
{
    private const double BrowserAddonRefreshIntervalMilliseconds = 2000;

    private readonly IOutboundPortBrowserFactory _browserFactory;

    private readonly DispatcherTimer _timer;


    public ObservableCollection<ViewModelBrowserAddonStatus> Browsers { get; } = [];

    public ViewModelInstallAddOn(
        IOutboundPortBrowserFactory browserFactory,
        IInboundPortLocalizationProvider localizationService)
    {
        ArgumentNullException.ThrowIfNull(browserFactory);
        ArgumentNullException.ThrowIfNull(localizationService);

        _browserFactory = browserFactory;

        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Brave), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Chrome), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Edge), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Firefox), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Vivaldi), localizationService));

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(BrowserAddonRefreshIntervalMilliseconds)
        };
        _timer.Tick += OnBrowserAddonRefreshTimerTick;
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnBrowserAddonRefreshTimerTick;
    }

    private void OnBrowserAddonRefreshTimerTick(object? sender, object eventArgs)
    {
        foreach (ViewModelBrowserAddonStatus browserAddonStatus in Browsers)
        {
            browserAddonStatus.RefreshStatus();
        }
    }
}
