using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.Common.Attributes;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelInstalledBrowsers : ObservableObject, IDisposable
{
    private const int BrowserListRefreshIntervalSeconds = 2;

    private readonly IOutboundPortBrowserDiscoveryProvider _browserService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ViewModelInstalledBrowsers> _logger;
    private readonly IDialogService _dialogService;
    private readonly IUseCaseDownloadBrowser _downloadBrowserUseCase;
    private readonly IOutboundPortEVisitorConfigRepository _evRestarterConfigRepository;
    private readonly IInboundPortLocalizationProvider _localizationService;

    private readonly DispatcherTimer _refreshTimer;

    private volatile bool _disposed;

    private int _refreshInFlight;


    public ObservableCollection<ViewModelBrowserItem> Browsers { get; } = [];

    public ViewModelInstalledBrowsers(
        IOutboundPortBrowserDiscoveryProvider browserService,
        IDialogService dialogService,
        IUseCaseDownloadBrowser downloadBrowserUseCase,
        IInboundPortLocalizationProvider localizationService,
        ILoggerFactory loggerFactory,
        IOutboundPortEVisitorConfigRepository evRestarterConfigRepository)
    {
        ArgumentNullException.ThrowIfNull(browserService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(downloadBrowserUseCase);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(evRestarterConfigRepository);

        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ViewModelInstalledBrowsers>();

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

        LoadBrowsersSmartAsync().Forget(_logger, nameof(LoadBrowsersSmartAsync));

        _refreshTimer.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

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
                _localizationService,
                _loggerFactory.CreateLogger<ViewModelBrowserItem>());

            Browsers.Add(newBrowserItem);
        }
    }

    [ExceptionBoundary(ExceptionBoundaryKind.ThreadRoot, Justification = "Async void timer tick boundary for refreshing installed browsers.")]
    private async void OnRefreshTimerTick(object? sender, object eventArgs)
    {
        if (_disposed)
        {
            return;
        }

        if (Interlocked.Exchange(ref _refreshInFlight, 1) == 1)
        {
            return;
        }

        try
        {
            await LoadBrowsersSmartAsync();
        }
        catch (Exception exception)
        {
            LogLoadBrowsersFailed(exception);
        }
        finally
        {
            Volatile.Write(ref _refreshInFlight, 0);
        }
    }

    [LoggerMessage(
        EventId = LogEventIds.Browser.BrowserProfileDiscoveryFailed,
        Level = LogLevel.Error,
        Message = "Loading the installed browser list failed.")]
    private partial void LogLoadBrowsersFailed(Exception exception);
}
