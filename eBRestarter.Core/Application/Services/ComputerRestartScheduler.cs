using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Services
{
    public class ComputerRestartScheduler : IComputerRestartScheduler, IDisposable
    {
        private readonly IEVisitorConfigService _configService;
        private readonly IWindowsProcessControlService _processService;
        private readonly ILogger<ComputerRestartScheduler> _logger;

        private PeriodicTimer? _timer;
        private Task? _backgroundTask;
        private CancellationTokenSource? _cts;

        public ComputerRestartScheduler(
            IEVisitorConfigService configService,
            IWindowsProcessControlService processService,
            ILogger<ComputerRestartScheduler> logger)
        {
            _configService = configService;
            _processService = processService;
            _logger = logger;
        }

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
                _cts.Cancel();
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
                    CheckAndExecuteRestart();
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

        private void CheckAndExecuteRestart()
        {
            var config = _configService.LoadConfig();

            if (config.Computer.NextRestartDate == null)
            {
                return;
            }

            var today = DateTime.Today;
            var now = DateTime.Now;
            var restartDate = config.Computer.NextRestartDate.Value.Date;


            bool isCorrectDay = restartDate == today;

            bool isCorrectTime = now.Hour == config.Computer.RestartClockTime && now.Minute == 0;

            if (isCorrectDay && isCorrectTime)
            {
            }
        }

        private void ExecuteRestartSequence()
        {
            _logger.LogWarning("Automatischer Neustart wird eingeleitet...");

            try
            {
                _logger.LogInformation("Schließe offene Anwendungen...");
                _processService.CloseAllOpenPrograms();

                Thread.Sleep(3000);

                _logger.LogInformation("Fahre System herunter...");
                _processService.ShutdownComputer();

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Ausführen der Neustart-Sequenz.");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _cts?.Dispose();
        }
    }
}
