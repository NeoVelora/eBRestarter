using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Enums;
using System.Diagnostics;

namespace eBRestarter.Core.Application.UseCases.ManageRestarterCycle;

public class ManageRestarterCycleService(
    IBrowserFactory browserFactory,
    ILocalizationService localizationService,
    IBrowserDisplayNameResolver browserDisplayNameResolver,
    IEVisitorConfigService configService,
    IBrowserCleanupScheduleService browserCleanupScheduleService,
    TimeProvider timeProvider,
    IWindowsProcessControlService processService) : IManageRestarterCycleUseCase
{
    private const string BaseUrl = WebLinks.EVisitorSurflink;
    private const int InitialDelaySeconds = 5;

    private readonly IBrowserFactory _browserFactory = browserFactory;
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly IBrowserDisplayNameResolver _browserDisplayNameResolver = browserDisplayNameResolver;
    private readonly IEVisitorConfigService _configService = configService;
    private readonly IBrowserCleanupScheduleService _browserCleanupScheduleService = browserCleanupScheduleService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IWindowsProcessControlService _processService = processService;

    private CancellationTokenSource? _cts;
    private IBrowser? _currentBrowser;

    public event EventHandler<RestarterCycleProgress>? ProgressChanged;

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
            var appConfig = _configService.LoadConfig();

            request = request with
            {
                BrowserDisplayName = string.IsNullOrWhiteSpace(appConfig.Browser?.Selected) ? request.BrowserDisplayName : appConfig.Browser.Selected,
                Username = string.IsNullOrWhiteSpace(appConfig.Username) ? request.Username : appConfig.Username,
                RuntimeSeconds = appConfig.Browser != null ? appConfig.Browser.RuntimeHours * 3600 : request.RuntimeSeconds,
                PauseSeconds = appConfig.Browser?.RuntimePauseSeconds ?? request.PauseSeconds,
                CheckBrowserAliveRoutine = appConfig.Browser?.CheckBrowserAliveRoutine ?? request.CheckBrowserAliveRoutine
            };

            LaunchBrowser(request);

            var phaseResult = await RunBrowserPhaseAsync(request, token);

            if (phaseResult == BrowserPhaseResult.BrowserClosed)
            {
                CloseCurrentBrowser();

                for (int countdown = 5; countdown > 0; countdown--)
                {
                    token.ThrowIfCancellationRequested();
                    ReportProgress(RestartTaskState.Cooldown, countdown, $"Der Browser wurde geschlossen, Starte Restarter in {countdown}...");
                    await Task.Delay(TimeSpan.FromMilliseconds(1000), _timeProvider, token);
                }
                continue;
            }

            await CheckAndExecuteBrowserCleanupAsync(performCleanupCallback, token);

            CloseCurrentBrowser();
            await RunDelayPhaseAsync(RestartTaskState.Cooldown, request.PauseSeconds, token);
        }
    }

    private async Task<BrowserPhaseResult> RunBrowserPhaseAsync(ManageRestarterCycleRequest request, CancellationToken token)
    {
        string processName = GetProcessNameFromDisplayName(request.BrowserDisplayName);

        var appConfig = _configService.LoadConfig();
        bool isCleanupActive = appConfig.Browser != null && appConfig.Browser.DeleteBrowserCacheIntervalDays > 0;
        DateTime nextCleanupDate = appConfig.Browser?.NextBrowserDeleteCacheDate ?? DateTime.MaxValue;

        for (int secondsRemaining = request.RuntimeSeconds; secondsRemaining >= 0; secondsRemaining--)
        {
            token.ThrowIfCancellationRequested();

            if (request.CheckBrowserAliveRoutine && secondsRemaining < request.RuntimeSeconds - 2)
            {
                if (!_processService.IsProcessAlive(processName))
                {
                    return BrowserPhaseResult.BrowserClosed;
                }
            }

            if (isCleanupActive && _timeProvider.GetLocalNow().DateTime >= nextCleanupDate)
            {
                return BrowserPhaseResult.CleanupDue;
            }

            ReportProgress(RestartTaskState.Running, secondsRemaining);

            if (secondsRemaining > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1000), _timeProvider, token);
            }
        }

        return BrowserPhaseResult.Completed;
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

    private string GetProcessNameFromDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "";
        var lower = displayName.ToLowerInvariant();

        if (lower.Contains("firefox")) return "firefox";
        if (lower.Contains("chrome")) return "chrome";
        if (lower.Contains("edge")) return "msedge";
        if (lower.Contains("vivaldi")) return "vivaldi";
        if (lower.Contains("brave")) return "brave";
        if (lower.Contains("opera")) return "opera";

        return lower;
    }
}