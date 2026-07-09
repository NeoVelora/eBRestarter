using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using System;
using System.Collections.ObjectModel;
using System.Timers;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelInstallAddOn : ObservableObject, IDisposable
{
    private const double BrowserAddonRefreshIntervalMilliseconds = 2000;

    private readonly IOutboundPortBrowserFactory _browserFactory;

    private readonly Timer _timer;

    public ObservableCollection<ViewModelBrowserAddonStatus> Browsers { get; } = [];


    public ViewModelInstallAddOn(IOutboundPortBrowserFactory BrowserFactory, IInboundPortLocalizationProvider LocalizationProvider)
    {
        ArgumentNullException.ThrowIfNull(BrowserFactory);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);

        _browserFactory = BrowserFactory;

        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Brave), LocalizationProvider));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Chrome), LocalizationProvider));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Edge), LocalizationProvider));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Firefox), LocalizationProvider));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Vivaldi), LocalizationProvider));

        _timer = new Timer(BrowserAddonRefreshIntervalMilliseconds);
        _timer.Elapsed += OnBrowserAddonRefreshTimerElapsed;
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Elapsed -= OnBrowserAddonRefreshTimerElapsed;
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnBrowserAddonRefreshTimerElapsed(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        foreach (ViewModelBrowserAddonStatus browserAddonStatus in Browsers)
            browserAddonStatus.RefreshStatus();
    }
}


