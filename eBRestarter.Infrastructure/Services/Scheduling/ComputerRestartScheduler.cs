using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Services.Scheduling;

public partial class ComputerRestartScheduler(
    IEVisitorConfigService configService,
    IWindowsProcessControlService processService,
    IApplicationLifetime applicationLifetime,
    TimeProvider timeProvider,
    ILogger<ComputerRestartScheduler> logger) : IComputerRestartScheduler, IDisposable
{
    private readonly IEVisitorConfigService _configService = configService;
    private readonly IWindowsProcessControlService _processService = processService;
    private readonly IApplicationLifetime _applicationLifetime = applicationLifetime;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<ComputerRestartScheduler> _logger = logger;

    private PeriodicTimer? _timer;
    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;

    public event EventHandler<DateTime?>? OnNextRestartDateChanged;

    public void StartScheduler()
    {
        if (_backgroundTask != null) return;

        _logger.LogInformation("Computer Restart Scheduler gestartet.");

        _cts = new CancellationTokenSource();

        _timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        _backgroundTask = Task.Run(async () => await LoopAsync(_cts.Token));
    }

    public async Task StopSchedulerAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException) { }

            _backgroundTask = null;
        }

        _logger.LogInformation("Computer Restart Scheduler gestoppt.");
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
            _logger.LogError(ex, "Fehler im Restart-Scheduler Loop.");
        }
    }

    private async Task CheckAndExecuteRestartAsync()
    {
        var config = _configService.LoadConfig();

        if (config.Computer.NextRestartDate == null || config.Computer.ComputerRestartIntervalDays <= 0)
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
                _logger.LogWarning("Geplanter Neustart am {Target} wurde verpasst. Berechne neuen Termin...", targetDateTime);

                DateTime newTargetDate;

                if (now.Hour >= config.Computer.RestartClockTime)
                {
                    newTargetDate = today.AddDays(config.Computer.ComputerRestartIntervalDays);
                }
                else
                {
                    newTargetDate = today;
                }

                config.Computer.NextRestartDate = newTargetDate;
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
        _logger.LogWarning("Automatischer Neustart wird eingeleitet...");

        try
        {
            _logger.LogInformation("Schließe offene Anwendungen...");
            await _processService.CloseAllOpenProgramsAsync(30000);

            _logger.LogInformation("Fahre System herunter...");
            _processService.ShutdownComputer();

            _applicationLifetime.ExitApplication(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Ausführen der Neustart-Sequenz.");
        }
    }

    private bool _disposed;
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
}
