using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Enums;
using System;
using System.Collections.ObjectModel;
using System.Timers;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public partial class ViewModelInstallAddOn : ObservableObject, IDisposable
{
    private const double BrowserAddonRefreshIntervalMilliseconds = 2000;

    private readonly IBrowserFactory _browserFactory;

    private readonly Timer _timer;

    public ObservableCollection<ViewModelBrowserAddonStatus> Browsers { get; } = [];


    public ViewModelInstallAddOn(IBrowserFactory browserFactory, ILocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(browserFactory);
        ArgumentNullException.ThrowIfNull(localizationService);

        _browserFactory = browserFactory;

        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Brave), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Chrome), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Edge), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Firefox), localizationService));
        Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Vivaldi), localizationService));

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
