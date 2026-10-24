using System;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Common.Attributes;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Inbound.Services;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.Logging;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

namespace eBRestarter.Core.Application.BehavioralComponents.Services;

public sealed class ComputerRestartService(
    IOutboundPortApplicationLifetime applicationLifetime,
    IOutboundPortEVisitorConfigRepository configService,
    IOutboundPortApplicationLogger<ComputerRestartService> logger,
    IOutboundPortOsProcessControl processService,
    TimeProvider timeProvider) : IInboundPortComputerRestartService, IDisposable
{
    private const int ApplicationExitSuccessCode = 0;
    private const int MissedSlotToleranceMinutes = 5;
    private const int ProcessCloseTimeoutMilliseconds = 30000;
    private static readonly TimeSpan SchedulerTimerInterval = TimeSpan.FromSeconds(30);

    private readonly IOutboundPortApplicationLifetime _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
    private readonly IOutboundPortEVisitorConfigRepository _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    private readonly IOutboundPortApplicationLogger<ComputerRestartService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOutboundPortOsProcessControl _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private bool _disposed;

    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;
    private readonly Lock _syncLock = new();
    private PeriodicTimer? _timer;


    public event EventHandler<DateTime?>? NextRestartDateChanged;

    public void StartScheduler()
    {
        lock (_syncLock)
        {
            if (_backgroundTask is not null)
                return;

            _logger.LogInformation(LogEventIds.RestarterCycle.RestartSchedulerStarted, "Computer Restart Scheduler started.");

            _cts = new CancellationTokenSource();
            _timer = new PeriodicTimer(SchedulerTimerInterval, _timeProvider);

            PeriodicTimer timer = _timer;
            CancellationToken token = _cts.Token;

            _backgroundTask = Task.Run(() => LoopAsync(timer, token));
        }
    }

    public async Task StopSchedulerAsync()
    {
        CancellationTokenSource? cts;
        Task? backgroundTask;
        PeriodicTimer? timer;

        lock (_syncLock)
        {
            if (_backgroundTask is null)
                return;

            cts = _cts;
            backgroundTask = _backgroundTask;
            timer = _timer;

            _cts = null;
            _backgroundTask = null;
            _timer = null;
        }

        var cancellationToken = cts?.Token ?? CancellationToken.None;

        if (cts is not null)
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }

        timer?.Dispose();

        if (backgroundTask is not null)
        {
            try
            {
                await backgroundTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug(
                    LogEventIds.RestarterCycle.RestartSchedulerStopped,
                    "Restart scheduler background task was cancelled during shutdown.");
            }
            finally
            {
                cts?.Dispose();
            }
        }
        else
        {
            cts?.Dispose();
        }

        _logger.LogInformation(LogEventIds.RestarterCycle.RestartSchedulerStopped, "Computer Restart Scheduler stopped.");
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_disposed)
                return;

            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();
            _timer?.Dispose();
        }
    }

    private void OnNextRestartDateChanged(DateTime? nextRestartDate)
    {
        NextRestartDateChanged?.Invoke(this, nextRestartDate);
    }

    [ExceptionBoundary(ExceptionBoundaryKind.BackgroundLoop, Justification = "Top-level boundary for the periodic computer restart scheduler loop.")]
    private async Task LoopAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await CheckAndExecuteRestartAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                LogEventIds.RestarterCycle.RestartSchedulerStopped,
                "Restart scheduler loop was requested to terminate.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                LogEventIds.RestarterCycle.RestartSchedulerLoopFaulted,
                ex,
                "An unhandled fault occurred within the restart scheduler loop runtime.");
        }
    }

    private async Task CheckAndExecuteRestartAsync()
    {
        var appConfig = _configService.LoadConfig();

        if (appConfig.Computer.NextRestartDate is null || appConfig.Computer.ComputerRestartIntervalDays <= 0)
            return;

        var now = _timeProvider.GetLocalNow();
        var targetDateTime = appConfig.Computer.NextRestartDate.Value.Date.AddHours(appConfig.Computer.RestartClockTime);

        if (now < targetDateTime)
            return;

        var today = now.Date;

        if (now > targetDateTime.AddMinutes(MissedSlotToleranceMinutes))
        {
            _logger.LogWarning(
                LogEventIds.RestarterCycle.RestartSlotMissed,
                "Missed the targeted automated computer restart slot scheduled at {Target}. Recalculating new target execution window...",
                targetDateTime);

            DateTime newTargetDate = now.Hour >= appConfig.Computer.RestartClockTime
                ? today.AddDays(appConfig.Computer.ComputerRestartIntervalDays).AddHours(appConfig.Computer.RestartClockTime)
                : today.AddHours(appConfig.Computer.RestartClockTime);

            appConfig.Computer.SetNextRestartDate(newTargetDate);
            _configService.SaveConfig(appConfig);

            OnNextRestartDateChanged(newTargetDate);
            return;
        }

        var restartDate = appConfig.Computer.NextRestartDate.Value.Date;
        bool isCorrectDay = restartDate == today;
        bool isCorrectTime = now.Hour == appConfig.Computer.RestartClockTime;

        if (isCorrectDay && isCorrectTime)
        {
            await ExecuteRestartSequenceAsync().ConfigureAwait(false);
        }
    }

    [ExceptionBoundary(ExceptionBoundaryKind.BackgroundLoop, Justification = "Terminal boundary for the computer shutdown and hardware reboot sequence.")]
    private async Task ExecuteRestartSequenceAsync()
    {
        _logger.LogWarning(
            LogEventIds.RestarterCycle.RestartSequenceInitiated,
            "Initiating automated computer system hardware restart sequence...");

        try
        {
            _logger.LogInformation(
                LogEventIds.RestarterCycle.RestartClosingApplications,
                "Requesting graceful closure across all active desktop applications windows...");
            await _processService.CloseAllOpenProgramsAsync(ProcessCloseTimeoutMilliseconds).ConfigureAwait(false);

            _logger.LogInformation(
                LogEventIds.RestarterCycle.RestartDispatchingReboot,
                "Dispatching system level hardware reboot instruction sets...");
            _processService.ShutdownComputer();

            _applicationLifetime.ExitApplication(ApplicationExitSuccessCode);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                LogEventIds.RestarterCycle.RestartSequenceFailed,
                ex,
                "A fatal fault aborted the smooth processing execution tracking tasks during the machine reboot phase sequence.");
        }
    }
}
