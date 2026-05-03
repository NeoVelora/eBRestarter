using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

public class WindowsStartupService(
    ILogger<WindowsStartupService> logger,
    IWindowsRegistryService registry,
    IProcessInfoService processInfo) : IWindowsStartupManagerService
{
#pragma warning disable RCS1213 // Remove unused member declaration
    private const string StartupTaskId = "eBRestarterAutoStart";
#pragma warning restore RCS1213 // Remove unused member declaration


    private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";

    private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

    private readonly ILogger<WindowsStartupService> _logger = logger;
    private readonly IWindowsRegistryService _registry = registry;
    private readonly IProcessInfoService _processInfo = processInfo;

    public void EnableAutoStart()
    {
        try
        {
            string exePath = _processInfo.GetCurrentExecutablePath();
            _registry.SetCurrentUserValue(RegistryPathRun, "eBRestarter", exePath);
            _logger.LogInformation("Autostart entry for 'eBRestarter' successfully created.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting autostart in registry.");
        }
    }

    public void DisableAutoStart()
    {
        try
        {
            _registry.DeleteCurrentUserValue(RegistryPathRun, "eBRestarter");
            _logger.LogInformation("Autostart entry for 'eBRestarter' removed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing autostart from registry.");
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
            _logger.LogError(ex, "Error retrieving autostart entries.");

            return [];
        }
    }

    public void SetEdgeStartupBoost(bool enable)
    {
        try
        {
            int dwordValue = enable ? 1 : 0;

            _registry.SetLocalMachineValue(RegistryPathEdgePolicies, "StartupBoostEnabled", dwordValue, RegistryValueKind.DWord);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Edge Startup Boost set to {State} (Value: {Value}).", enable, dwordValue);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when changing Edge policies. Please run as Administrator.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General error setting Edge Startup Boost.");
        }
    }

    public bool IsEdgeStartupBoostEnabled()
    {
        try
        {
            var value = _registry.GetLocalMachineValue(RegistryPathEdgePolicies, "StartupBoostEnabled");

            if (value is int intValue)
            {
                return intValue == 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read Edge Startup Boost status.");
        }

        return true;
    }

    public void SetAutoLogon(bool enable)
    {
        try
        {
            int dwordValue = enable ? 0 : 2;
            _registry.SetLocalMachineValue(RegistryPathPasswordLess, "DevicePasswordLessBuildVersion", dwordValue, RegistryValueKind.DWord);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("AutoLogon setting set to {State} (RegValue: {Value}).", enable, dwordValue);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when changing PasswordLess settings. Please run as Administrator.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring AutoLogon settings.");
        }
    }


    public Task EnableAutoStartAsync()
    {
        EnableAutoStart();
        return Task.CompletedTask;
    }

    public Task DisableAutoStartAsync()
    {
        DisableAutoStart();
        return Task.CompletedTask;
    }

    public Task<bool> IsAutoStartEnabledAsync()
    {
        try
        {
            var entries = GetStartupEntries();
            bool isEnabled = entries.ContainsKey("eBRestarter");

            return Task.FromResult(isEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Prüfen des Autostart-Status.");
            return Task.FromResult(false);
        }
    }
}