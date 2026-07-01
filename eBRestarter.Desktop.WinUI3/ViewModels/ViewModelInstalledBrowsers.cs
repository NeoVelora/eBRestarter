using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Extensions;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.DownloadBrowser;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelInstalledBrowsers : ObservableObject, IDisposable
{
    private const int BrowserListRefreshIntervalSeconds = 2;

    private readonly IBrowserDiscoveryProviderOutboundPort _browserService;

    private readonly IDownloadBrowserUseCase _downloadBrowserUseCase;

    private readonly IDialogService _dialogService;

    private readonly IEVisitorConfigRepositoryOutboundPort _EVRestarterConfigRepository;

    private readonly ILocalizationProvider _localizationService;

    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty]
    public partial ObservableCollection<ViewModelBrowserItem> Browsers { get; set; } = [];


    public ViewModelInstalledBrowsers(
        IBrowserDiscoveryProviderOutboundPort browserService,
        IDownloadBrowserUseCase downloadBrowserUseCase,
        IDialogService dialogService,
        ILocalizationProvider LocalizationProvider,
        IEVisitorConfigRepositoryOutboundPort EVRestarterConfigRepository)
    {
        ArgumentNullException.ThrowIfNull(browserService);
        ArgumentNullException.ThrowIfNull(downloadBrowserUseCase);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);
        ArgumentNullException.ThrowIfNull(EVRestarterConfigRepository);

        _browserService = browserService;
        _downloadBrowserUseCase = downloadBrowserUseCase;
        _dialogService = dialogService;
        _localizationService = LocalizationProvider;
        _EVRestarterConfigRepository = EVRestarterConfigRepository;

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
                    _downloadBrowserUseCase,
                    _EVRestarterConfigRepository,
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


