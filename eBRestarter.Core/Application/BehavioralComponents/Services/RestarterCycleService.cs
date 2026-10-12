using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.BehavioralComponents.Handlers.Interfaces;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Services;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Validators;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;
using eBRestarter.Core.Domain.Handlers;

namespace eBRestarter.Core.Application.BehavioralComponents.Services;

public sealed class RestarterCycleService(
    IBrowserCleanupScheduleHandler browserCleanupScheduleHandler,
    IOutboundPortBrowserFactory browserFactory,
    IOutboundPortEVisitorConfigRepository configService,
    IDelayPhaseHandler delayPhaseHandler,
    IInboundPortLocalizationProvider localizationService,
    IOutboundPortApplicationLogger<RestarterCycleService> logger,
    IRunBrowserPhaseHandler runBrowserPhaseHandler,
    TimeProvider timeProvider,
    IInboundPortApplicationValidator<ManageRestarterCycleRequest> validator) : IInboundPortRestarterCycleService
{
    private const string BaseUrl = "https://www.ebesucher.de/surfbar/";
    private const int CleanupDelayMilliseconds = 1000;
    private const string BrowserStartFailedMessage = "Der Browser konnte nicht gestartet werden.";
    private const string BrowserStartRetryFormat = "Browser nicht startbar. Neuer Versuch in {0}...";

    private const string CycleScopePropertyName = "RestarterCycleId";
    private const int EmergencyCooldownSeconds = 5;
    private const int InitialDelaySeconds = 5;
    private const string LocalizationKeyErrorPrefix = "General_ErrorPrefix";
    private const string LocalizationKeyStatusRestartIn = "Task_StatusRestartIn";
    private const string LocalizationKeyStatusRunning = "Task_StatusRunning";
    private const string LocalizationKeyStatusStartIn = "Task_StatusStartIn";
    private const string LocalizationKeyStatusStopped = "Task_StatusStopped";
    private const int SecondsPerHour = 3600;

    private readonly IBrowserCleanupScheduleHandler _browserCleanupScheduleHandler = browserCleanupScheduleHandler ?? throw new ArgumentNullException(nameof(browserCleanupScheduleHandler));
    private readonly IOutboundPortBrowserFactory _browserFactory = browserFactory ?? throw new ArgumentNullException(nameof(browserFactory));
    private readonly IOutboundPortEVisitorConfigRepository _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    private readonly IDelayPhaseHandler _delayPhaseHandler = delayPhaseHandler ?? throw new ArgumentNullException(nameof(delayPhaseHandler));
    private readonly IInboundPortLocalizationProvider _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    private readonly IOutboundPortApplicationLogger<RestarterCycleService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRunBrowserPhaseHandler _runBrowserPhaseHandler = runBrowserPhaseHandler ?? throw new ArgumentNullException(nameof(runBrowserPhaseHandler));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly IInboundPortApplicationValidator<ManageRestarterCycleRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    private static readonly CompositeFormat BrowserStartRetryCompositeFormat = CompositeFormat.Parse(BrowserStartRetryFormat);

    private CancellationTokenSource? _cts;
    private IOutboundPortBrowser? _currentBrowser;

    private CompositeFormat? _statusRestartInFormat;
    private CompositeFormat? _statusStartInFormat;

    private readonly Lock _syncLock = new();

    public event EventHandler<RestarterCycleProgress>? ProgressChanged;

    public async Task StartAsync(
        ManageRestarterCycleRequest request,
        Func<Task> performCleanupCallback)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(performCleanupCallback);

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            ReportProgress(RestartTaskState.Idle, 0, validationResult.Errors[0].ErrorMessage);
            return;
        }

        Stop();

        CancellationTokenSource cts;
        lock (_syncLock)
        {
            _cts = new CancellationTokenSource();
            cts = _cts;
        }

        var cancellationToken = cts.Token;

        using var cycleScope = _logger.BeginScope(CycleScopePropertyName, Guid.NewGuid().ToString("N"));

        try
        {
            await RunCycleAsync(request, performCleanupCallback, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                LogEventIds.RestarterCycle.RestartSchedulerStopped,
                "Restarter cycle was cancelled by an explicit stop request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                LogEventIds.RestarterCycle.CycleFaulted,
                ex,
                "The restarter cycle aborted with an unhandled fault.");
            ReportProgress(RestartTaskState.Idle, 0, $"Fehler: {ex.Message}");
        }
        finally
        {
            CloseCurrentBrowser();
            ReportProgress(RestartTaskState.Idle, 0);
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        lock (_syncLock)
        {
            cts = _cts;
            _cts = null;
        }

        if (cts is not null)
        {
            _ = cts.CancelAsync().ContinueWith(
                _ => cts.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private async Task RunCycleAsync(
        ManageRestarterCycleRequest request,
        Func<Task> performCleanupCallback,
        CancellationToken cancellationToken)
    {
        await _delayPhaseHandler.ExecuteAsync(RestartTaskState.InitialDelay, InitialDelaySeconds, ReportProgress, cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            var appConfig = _configService.LoadConfig();

            request = request with
            {
                BrowserType = appConfig.Browser != null
                    && !string.IsNullOrWhiteSpace(appConfig.Browser.Selected)
                    && Enum.TryParse<BrowserType>(appConfig.Browser.Selected, true, out var parsedType)
                        ? parsedType
                        : request.BrowserType,
                Username = string.IsNullOrWhiteSpace(appConfig.Username) ? request.Username : appConfig.Username,
                RuntimeSeconds = appConfig.Browser is not null ? appConfig.Browser.RuntimeHours * SecondsPerHour : request.RuntimeSeconds,
                PauseSeconds = appConfig.Browser?.RuntimePauseSeconds ?? request.PauseSeconds,
                CheckBrowserAliveRoutine = appConfig.Browser?.CheckBrowserAliveRoutine ?? request.CheckBrowserAliveRoutine
            };

            if (!LaunchBrowser(request))
            {
                await _delayPhaseHandler.ExecuteAsync(
                    RestartTaskState.Cooldown,
                    EmergencyCooldownSeconds,
                    (state, secondsRemaining, _) => ReportProgress(
                        state,
                        secondsRemaining,
                        string.Format(
                            CultureInfo.CurrentCulture,
                            BrowserStartRetryCompositeFormat,
                            secondsRemaining)),
                    cancellationToken).ConfigureAwait(false);

                continue;
            }

            var runPhaseResult = await _runBrowserPhaseHandler.ExecuteAsync(request, _currentBrowser, ReportProgress, cancellationToken).ConfigureAwait(false);

            if (runPhaseResult == BrowserPhaseResult.BrowserClosed)
            {
                CloseCurrentBrowser();

                await _delayPhaseHandler.ExecuteAsync(
                    RestartTaskState.Cooldown,
                    EmergencyCooldownSeconds,
                    (state, secondsRemaining, _) =>
                    {
                        string msg = $"Der Browser wurde geschlossen, Starte Restarter in {secondsRemaining}...";
                        ReportProgress(state, secondsRemaining, msg);
                    },
                    cancellationToken).ConfigureAwait(false);

                continue;
            }

            await CheckAndExecuteBrowserCleanupAsync(performCleanupCallback, cancellationToken).ConfigureAwait(false);
            CloseCurrentBrowser();
            await _delayPhaseHandler.ExecuteAsync(RestartTaskState.Cooldown, request.PauseSeconds, ReportProgress, cancellationToken).ConfigureAwait(false);
        }
    }

    private bool LaunchBrowser(ManageRestarterCycleRequest request)
    {
        var browserType = request.BrowserType;

        _currentBrowser = _browserFactory.Create(browserType);
        string url = $"{BaseUrl}{Uri.EscapeDataString(request.Username)}";

        if (_currentBrowser.Start(url))
        {
            return true;
        }


        var errorFormat = _localizationService.RetrieveString(LocalizationKeyErrorPrefix);
        var errorMsg = string.Format(CultureInfo.CurrentCulture, errorFormat, BrowserStartFailedMessage);

        ReportProgress(RestartTaskState.Idle, 0, errorMsg);

        CloseCurrentBrowser();

        return false;
    }

    private void CloseCurrentBrowser()
    {
        _currentBrowser?.Close();
        _currentBrowser = null;
    }

    private async Task CheckAndExecuteBrowserCleanupAsync(
        Func<Task> performCleanupCallback,
        CancellationToken cancellationToken)
    {
        var appConfig = _configService.LoadConfig();
        var browser = appConfig.Browser;

        if (browser is null || !_browserCleanupScheduleHandler.ShouldRunCleanupNow(browser.DeleteBrowserCacheIntervalDays, browser.NextBrowserDeleteCacheDate))
            return;

        CloseCurrentBrowser();

        await Task.Delay(TimeSpan.FromMilliseconds(CleanupDelayMilliseconds), _timeProvider, cancellationToken).ConfigureAwait(false);

        await performCleanupCallback().ConfigureAwait(false);

        var today = _timeProvider.GetLocalNow().Date;
        var newDate = _browserCleanupScheduleHandler.CalculateNextCleanupDateAfterRun(today, appConfig.Browser.DeleteBrowserCacheIntervalDays);

        appConfig.Browser.SetNextCleanupDate(newDate);
        _configService.SaveConfig(appConfig);
    }

    private void ReportProgress(RestartTaskState state, int secondsRemaining, string? customMessage = null)
    {
        string statusMessage = customMessage ?? RetrieveStatusMessageForState(state, secondsRemaining);

        ProgressChanged?.Invoke(this, new RestarterCycleProgress(state, secondsRemaining, statusMessage));
    }

    private string RetrieveStatusMessageForState(RestartTaskState state, int secondsRemaining)
    {
        return state switch
        {
            RestartTaskState.Idle => _localizationService.RetrieveString(LocalizationKeyStatusStopped),
            RestartTaskState.InitialDelay => string.Format(
                CultureInfo.CurrentCulture,
                _statusStartInFormat ??= CompositeFormat.Parse(_localizationService.RetrieveString(LocalizationKeyStatusStartIn)),
                secondsRemaining),
            RestartTaskState.Running => _localizationService.RetrieveString(LocalizationKeyStatusRunning),
            RestartTaskState.Cooldown => string.Format(
                CultureInfo.CurrentCulture,
                _statusRestartInFormat ??= CompositeFormat.Parse(_localizationService.RetrieveString(LocalizationKeyStatusRestartIn)),
                secondsRemaining),
            _ => string.Empty
        };
    }
}