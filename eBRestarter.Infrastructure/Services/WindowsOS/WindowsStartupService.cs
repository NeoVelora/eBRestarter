using eBRestarter.Application.Services.Ports.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Serilog;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS
{
    [SupportedOSPlatform("windows")]
    public class WindowsStartupService : IStartupManagerService
    {
        private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";

        private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

        private readonly ILogger<WindowsStartupService> _logger;

        public WindowsStartupService(ILogger<WindowsStartupService> logger)
        {
            _logger = logger;
        }

        public void EnableAutoStart()
        {
            try
            {
                using var rkApp = Registry.CurrentUser.OpenSubKey(RegistryPathRun, true);

                if (rkApp != null)
                {
                    rkApp.SetValue("eV Restarter", Process.GetCurrentProcess().MainModule!.FileName);
                    _logger.LogInformation("Autostart entry for 'eV Restarter' created.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set Autostart in registry.");
            }
        }

        public void DisableAutoStart()
        {
            try
            {
                using var rkApp = Registry.CurrentUser.OpenSubKey(RegistryPathRun, true);

                rkApp?.DeleteValue("eV Restarter", false);
                _logger.LogInformation("Autostart entry for 'eV Restarter' removed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove Autostart from registry.");
            }
        }

        public Dictionary<string, object> GetStartupEntries()
        {
            var valuesBynames = new Dictionary<string, object>();

            try
            {
                using var rootKey = Registry.CurrentUser.OpenSubKey(RegistryPathRun);

                if (rootKey != null)
                {
                    string[] valueNames = rootKey.GetValueNames();

                    foreach (string currSubKey in valueNames)
                    {
                        object value = rootKey.GetValue(currSubKey)!;
                        valuesBynames.Add(currSubKey, value);
                    }
                }

                return valuesBynames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve startup entries.");

                return new Dictionary<string, object>();
            }
        }

        public void SetEdgeStartupBoost(bool enable)
        {
            try
            {
                int dwordValue = enable ? 1 : 0;

                using RegistryKey key = Registry.LocalMachine.CreateSubKey(RegistryPathEdgePolicies);

                key.SetValue("StartupBoostEnabled", dwordValue, RegistryValueKind.DWord);

                _logger.LogInformation("Edge Startup Boost set to {State} ({Value}).", enable, dwordValue);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Permission denied modifying Edge Policies. Run application as Administrator.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Edge Startup Boost.");
            }
        }

        public void SetAutoLogon(bool enable)
        {
            try
            {
                int dwordValue = enable ? 0 : 2;

                using RegistryKey key = Registry.LocalMachine.CreateSubKey(RegistryPathPasswordLess);

                key.SetValue("DevicePasswordLessBuildVersion", dwordValue, RegistryValueKind.DWord);

                _logger.LogInformation("Windows PasswordLess Login requirement set to {State} (RegValue: {Value}).", !enable, dwordValue);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Permission denied modifying PasswordLess settings. Run application as Administrator.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring AutoLogon settings.");
            }
        }
    }
}
