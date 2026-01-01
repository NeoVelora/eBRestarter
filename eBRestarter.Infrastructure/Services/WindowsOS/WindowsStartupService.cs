using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Serilog;
using System.Diagnostics;
using System.Runtime.Versioning;
using Windows.ApplicationModel;

namespace eBRestarter.Infrastructure.Services.WindowsOS
{
    public class WindowsStartupService : IWindowsStartupManagerService
    {
        private const string StartupTaskId = "eBRestarterAutoStart";


        private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";

        private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

        private readonly ILogger<WindowsStartupService> _logger;

        private readonly IWindowsRegistryService _registry;

        private readonly IProcessInfoService _processInfo;

        public WindowsStartupService(
            ILogger<WindowsStartupService> logger,
            IWindowsRegistryService registry,
            IProcessInfoService processInfo)
        {
            _logger = logger;
            _registry = registry;
            _processInfo = processInfo;
        }

        public void EnableAutoStart()
        {
            try
            {
                string exePath = _processInfo.GetCurrentExecutablePath();

                _registry.SetCurrentUserValue(RegistryPathRun, "eBRestarter", exePath);

                _logger.LogInformation("Autostart-Eintrag für 'eBRestarter' erfolgreich erstellt.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Setzen des Autostarts in der Registry.");
            }
        }

        public void DisableAutoStart()
        {
            try
            {
                _registry.DeleteCurrentUserValue(RegistryPathRun, "eBRestarter");

                _logger.LogInformation("Autostart-Eintrag für 'eBRestarter' entfernt.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Entfernen des Autostarts aus der Registry.");
            }
        }

        public Dictionary<string, object> GetStartupEntries()
        {
            try
            {
                return _registry.GetCurrentUserValues(RegistryPathRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehlerbeim Abrufen der Autostart-Einträge.");
                return new Dictionary<string, object>();
            }
        }

        public void SetEdgeStartupBoost(bool enable)
        {
            try
            {
                int dwordValue = enable ? 1 : 0;

                _registry.SetLocalMachineValue(RegistryPathEdgePolicies, "StartupBoostEnabled", dwordValue, RegistryValueKind.DWord);

                _logger.LogInformation("Edge Startup Boost auf {State} gesetzt (Wert: {Value}).", enable, dwordValue);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Zugriff verweigert beim Ändern der Edge-Policies. Bitte als Administrator ausführen.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allgemeiner Fehler beim Setzen von Edge Startup Boost.");
            }
        }

        public void SetAutoLogon(bool enable)
        {
            try
            {
                int dwordValue = enable ? 0 : 2;

                _registry.SetLocalMachineValue(RegistryPathPasswordLess, "DevicePasswordLessBuildVersion", dwordValue, RegistryValueKind.DWord);

                _logger.LogInformation("AutoLogon-Einstellung auf {State} gesetzt (RegWert: {Value}).", enable, dwordValue);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Zugriff verweigert beim Ändern der PasswordLess-Einstellungen. Bitte als Administrator ausführen.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Konfigurieren der AutoLogon-Einstellungen.");
            }
        }


        public async Task EnableAutoStartAsync()
        {
            try
            {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);

                if (startupTask.State == StartupTaskState.Disabled)
                {
                    var newState = await startupTask.RequestEnableAsync();

                    if (newState == StartupTaskState.Enabled)
                        _logger.LogInformation("Autostart erfolgreich aktiviert.");
                    else
                        _logger.LogWarning("Autostart Aktivierung fehlgeschlagen (User abgelehnt?).");
                }
                else
                {
                    _logger.LogInformation($"Autostart ist bereits aktiv oder deaktiviert durch User. Status: {startupTask.State}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktivieren des Autostarts via StartupTask.");
            }
        }

        public async Task DisableAutoStartAsync()
        {
            try
            {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);

                startupTask.Disable();

                _logger.LogInformation("Autostart erfolgreich deaktiviert.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Deaktivieren des Autostarts.");
            }
        }

        public async Task<bool> IsAutoStartEnabledAsync()
        {
            try
            {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                return startupTask.State == StartupTaskState.Enabled;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
