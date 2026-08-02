using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using eBRestarter.Core.Application.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelInstalledBrowsers : ObservableObject, IDisposable
{
    private const int BrowserListRefreshIntervalSeconds = 2;

    private readonly IOutboundPortBrowserDiscoveryProvider _browserService;
    private readonly IDialogService _dialogService;
    private readonly IUseCaseDownloadBrowser _downloadBrowserUseCase;
    private readonly IOutboundPortEVisitorConfigRepository _evRestarterConfigRepository;
    private readonly IInboundPortLocalizationProvider _localizationService;

    private readonly DispatcherTimer _refreshTimer;


    public ObservableCollection<ViewModelBrowserItem> Browsers { get; } = [];

    public ViewModelInstalledBrowsers(
        IOutboundPortBrowserDiscoveryProvider browserService,
        IDialogService dialogService,
        IUseCaseDownloadBrowser downloadBrowserUseCase,
        IInboundPortLocalizationProvider localizationService,
        IOutboundPortEVisitorConfigRepository evRestarterConfigRepository)
    {
        ArgumentNullException.ThrowIfNull(browserService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(downloadBrowserUseCase);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(evRestarterConfigRepository);

        _browserService = browserService;
        _dialogService = dialogService;
        _downloadBrowserUseCase = downloadBrowserUseCase;
        _localizationService = localizationService;
        _evRestarterConfigRepository = evRestarterConfigRepository;

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

            if (existingBrowserItem is not null)
            {
                existingBrowserItem.Update(installedBrowserInfo);
                continue;
            }

            var newBrowserItem = new ViewModelBrowserItem(
                installedBrowserInfo,
                _downloadBrowserUseCase,
                _evRestarterConfigRepository,
                _dialogService,
                _localizationService);

            Browsers.Add(newBrowserItem);
        }
    }

    private void OnRefreshTimerTick(object? sender, object eventArgs)
    {
        LoadBrowsersSmartAsync().Forget();
    }
}
