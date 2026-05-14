using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Extensions;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public partial class ViewModelInstalledBrowsers : ObservableObject, IDisposable
{
    private const int BrowserListRefreshIntervalSeconds = 2;

    private readonly IBrowserService _browserService;

    private readonly IBrowserDownloadService _downloadService;

    private readonly IDialogService _dialogService;

    private readonly IEVisitorConfigService _eVisitorConfigService;

    private readonly ILocalizationService _localizationService;

    private readonly IOperatingSystemFacade _operatingSystemFacade;

    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty]
    public partial ObservableCollection<ViewModelBrowserItem> Browsers { get; set; } = [];


    public ViewModelInstalledBrowsers(
        IBrowserService browserService,
        IBrowserDownloadService downloadService,
        IOperatingSystemFacade operatingSystemFacade,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IEVisitorConfigService eVisitorConfigService)
    {
        ArgumentNullException.ThrowIfNull(browserService);
        ArgumentNullException.ThrowIfNull(downloadService);
        ArgumentNullException.ThrowIfNull(operatingSystemFacade);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(eVisitorConfigService);

        _browserService = browserService;
        _downloadService = downloadService;
        _operatingSystemFacade = operatingSystemFacade;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _eVisitorConfigService = eVisitorConfigService;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(BrowserListRefreshIntervalSeconds)
        };
        _refreshTimer.Tick += OnRefreshTimerTick;

        LoadBrowsersSmartAsync().Forget();

        _refreshTimer.Start();
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
        GC.SuppressFinalize(this);
    }

    public async Task LoadBrowsersSmartAsync()
    {
        var freshBrowserInfos = await _browserService.FindInstalledBrowsersAsync();

        foreach (var installedBrowserInfo in freshBrowserInfos)
        {
            var existingBrowserItem = Browsers.FirstOrDefault(browserItem => browserItem.BrowserType == installedBrowserInfo.Type);

            if (existingBrowserItem != null)
            {
                existingBrowserItem.Update(installedBrowserInfo);
            }
            else
            {
                var newBrowserItem = new ViewModelBrowserItem(
                    installedBrowserInfo,
                    _downloadService,
                    _operatingSystemFacade,
                    _eVisitorConfigService,
                    _dialogService,
                    _localizationService);
                Browsers.Add(newBrowserItem);
            }
        }
    }

    private void OnRefreshTimerTick(object? sender, object eventArgs)
    {
        LoadBrowsersSmartAsync().Forget();
    }
}
