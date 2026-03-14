using System;
using System.Threading;
using System.Threading.Tasks;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Domain.Enums;

namespace eBRestarter.Core.Application.UseCases.ManageRestarterCycle;

public class ManageRestarterCycleService : IManageRestarterCycleUseCase
{
    private const string BaseUrl = "https://www.ebesucher.com/surfbar/";
    private const int InitialDelaySeconds = 5;

    private readonly IBrowserFactory _browserFactory;
    private readonly ILocalizationService _localizationService;
    private readonly IBrowserDisplayNameResolver _browserDisplayNameResolver;
    private readonly IEVisitorConfigService _configService;
    private readonly IBrowserCleanupScheduleService _browserCleanupScheduleService;
    private readonly TimeProvider _timeProvider;

    private CancellationTokenSource? _cts;
    private IBrowser? _currentBrowser;
    
    public event EventHandler<RestarterCycleProgress>? ProgressChanged;

    public ManageRestarterCycleService(
        IBrowserFactory browserFactory,
        ILocalizationService localizationService,
        IBrowserDisplayNameResolver browserDisplayNameResolver,
        IEVisitorConfigService configService,
        IBrowserCleanupScheduleService browserCleanupScheduleService,
        TimeProvider timeProvider)
    {
        _browserFactory = browserFactory;
        _localizationService = localizationService;
        _browserDisplayNameResolver = browserDisplayNameResolver;
        _configService = configService;
        _browserCleanupScheduleService = browserCleanupScheduleService;
        _timeProvider = timeProvider;
    }

    public async Task StartAsync(ManageRestarterCycleRequest request, Func<Task> performCleanupCallback)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            await RunCycleAsync(request, performCleanupCallback, token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            CloseCurrentBrowser();
            ReportProgress(RestartTaskState.Idle, 0);
        }
    }

    public void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private async Task RunCycleAsync(ManageRestarterCycleRequest request, Func<Task> performCleanupCallback, CancellationToken token)
    {
        await RunDelayPhaseAsync(RestartTaskState.InitialDelay, InitialDelaySeconds, token);

        while (!token.IsCancellationRequested)
        {
            LaunchBrowser(request);
            await RunDelayPhaseAsync(RestartTaskState.Running, request.RuntimeSeconds, token);

            await CheckAndExecuteBrowserCleanupAsync(performCleanupCallback, token);

            CloseCurrentBrowser();
            await RunDelayPhaseAsync(RestartTaskState.Cooldown, request.PauseSeconds, token);
        }
    }

    private async Task RunDelayPhaseAsync(RestartTaskState state, int totalSeconds, CancellationToken token)
    {
        for (int secondsRemaining = totalSeconds; secondsRemaining >= 0; secondsRemaining--)
        {
            token.ThrowIfCancellationRequested();
            ReportProgress(state, secondsRemaining);

            if (secondsRemaining > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1000), _timeProvider, token);
            }
        }
    }

    private void LaunchBrowser(ManageRestarterCycleRequest request)
    {
        try
        {
            string defaultText = _localizationService.GetString("Task_DefaultBrowser");
            var browserType = _browserDisplayNameResolver.GetBrowserTypeFromDisplayName(request.BrowserDisplayName, defaultText);

            _currentBrowser = _browserFactory.Create(browserType);
            string url = $"{BaseUrl}{request.Username}";

            _currentBrowser.Start(url);
        }
        catch (Exception ex)
        {
            string errorFormat = _localizationService.GetString("General_ErrorPrefix");
            string errorMsg = string.Format(errorFormat, ex.Message);
            ReportProgress(RestartTaskState.Idle, 0, errorMsg);
            throw;
        }
    }

    private void CloseCurrentBrowser()
    {
        _currentBrowser?.Close();
        _currentBrowser = null;
    }

    private async Task CheckAndExecuteBrowserCleanupAsync(Func<Task> performCleanupCallback, CancellationToken token)
    {
        var appConfig = _configService.LoadConfig();

        if (!_browserCleanupScheduleService.ShouldRunCleanupNow(appConfig))
            return;

        CloseCurrentBrowser();

        await Task.Delay(TimeSpan.FromMilliseconds(1000), _timeProvider, token);

        await performCleanupCallback();

        var today = _timeProvider.GetLocalNow().Date;
        var newDate = _browserCleanupScheduleService.GetNextCleanupDateAfterRun(today, appConfig.Browser.DeleteBrowserCacheIntervalDays);
        appConfig.Browser.NextBrowserDeleteCacheDate = newDate;
        _configService.SaveConfig(appConfig);
    }

    private void ReportProgress(RestartTaskState state, int secondsRemaining, string? customMessage = null)
    {
        string statusMessage = customMessage ?? GetStatusMessageForState(state, secondsRemaining);
        ProgressChanged?.Invoke(this, new RestarterCycleProgress(state, secondsRemaining, statusMessage));
    }

    private string GetStatusMessageForState(RestartTaskState state, int secondsRemaining)
    {
        return state switch
        {
            RestartTaskState.Idle => _localizationService.GetString("Task_StatusStopped"),
            RestartTaskState.InitialDelay => string.Format(_localizationService.GetString("Task_StatusStartIn"), secondsRemaining),
            RestartTaskState.Running => _localizationService.GetString("Task_StatusRunning"),
            RestartTaskState.Cooldown => string.Format(_localizationService.GetString("Task_StatusRestartIn"), secondsRemaining),
            _ => string.Empty
        };
    }
}
