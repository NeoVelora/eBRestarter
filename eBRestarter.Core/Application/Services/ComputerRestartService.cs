using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Inbound.Services;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Core.Application.Services;

public class ComputerRestartService(
    IOutboundPortEVisitorConfigRepository configService,
    IOutboundPortOsProcessControl processService,
    IOutboundPortApplicationLifetime applicationLifetime,
    TimeProvider timeProvider,
    IOutboundPortApplicationLogger<ComputerRestartService> logger) : IInboundPortComputerRestartService, IDisposable
{
    private bool _disposed;

    private readonly IOutboundPortEVisitorConfigRepository _configService = configService;
    private readonly IOutboundPortOsProcessControl _processService = processService;
    private readonly IOutboundPortApplicationLifetime _applicationLifetime = applicationLifetime;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOutboundPortApplicationLogger<ComputerRestartService> _logger = logger;

    private PeriodicTimer? _timer;
    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;

    public event EventHandler<DateTime?>? OnNextRestartDateChanged;

    public void StartScheduler()
    {
        if (_backgroundTask is not null) return;

        _logger.LogInformation("Computer Restart Scheduler started.");

        _cts = new CancellationTokenSource();

        _timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        _backgroundTask = Task.Run(async () => await LoopAsync(_cts.Token));
    }

    public async Task StopSchedulerAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        if (_backgroundTask is not null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException) { }

            _backgroundTask = null;
        }

        _logger.LogInformation("Computer Restart Scheduler stopped.");
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _cts?.Dispose();
            }

            _disposed = true;
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(token))
            {
                await CheckAndExecuteRestartAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled fault occurred within the restart scheduler loop runtime.");
        }
    }

    private async Task CheckAndExecuteRestartAsync()
    {
        var config = _configService.LoadConfig();

        if (config.Computer.NextRestartDate is null || config.Computer.ComputerRestartIntervalDays <= 0)
        {
            return;
        }

        var now = _timeProvider.GetLocalNow();
        var targetDateTime = config.Computer.NextRestartDate.Value.Date.AddHours(config.Computer.RestartClockTime);
        var today = now.Date;

        if (now >= targetDateTime)
        {
            if (now > targetDateTime.AddMinutes(5))
            {
                _logger.LogWarning("Missed the targeted automated computer restart slot scheduled at {Target}. Recalculating new target execution window...", targetDateTime);

                DateTime newTargetDate;

                if (now.Hour >= config.Computer.RestartClockTime)
                {
                    newTargetDate = today.AddDays(config.Computer.ComputerRestartIntervalDays).AddHours(config.Computer.RestartClockTime);
                }
                else
                {
                    newTargetDate = today.AddHours(config.Computer.RestartClockTime);
                }

                config.Computer.SetNextRestartDate(newTargetDate);
                _configService.SaveConfig(config);

                OnNextRestartDateChanged?.Invoke(this, newTargetDate);
                return;
            }

            var restartDate = config.Computer.NextRestartDate.Value.Date;
            bool isCorrectDay = restartDate == today;
            bool isCorrectTime = now.Hour == config.Computer.RestartClockTime;

            if (isCorrectDay && isCorrectTime)
            {
                await ExecuteRestartSequenceAsync();
            }
        }
    }

    private async Task ExecuteRestartSequenceAsync()
    {
        _logger.LogWarning("Initiating automated computer system hardware restart sequence...");

        try
        {
            _logger.LogInformation("Requesting graceful closure across all active desktop applications windows...");
            await _processService.CloseAllOpenProgramsAsync(30000);

            _logger.LogInformation("Dispatching system level hardware reboot instruction sets...");
            _processService.ShutdownComputer();

            _applicationLifetime.ExitApplication(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A fatal fault aborted the smooth processing execution tracking tasks during the machine reboot phase sequence.");
        }
    }
}


