using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

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
            return new Dictionary<string, object>();
        }
    }

    public void SetEdgeStartupBoost(bool enable)
    {
        try
        {
            int dwordValue = enable ? 1 : 0;
            _registry.SetLocalMachineValue(RegistryPathEdgePolicies, "StartupBoostEnabled", dwordValue, RegistryValueKind.DWord);
            _logger.LogInformation("Edge Startup Boost set to {State} (Value: {Value}).", enable, dwordValue);
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
            _logger.LogInformation("AutoLogon setting set to {State} (RegValue: {Value}).", enable, dwordValue);
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


    public async Task EnableAutoStartAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);

            if (startupTask.State == StartupTaskState.Disabled)
            {
                var newState = await startupTask.RequestEnableAsync();

                if (newState == StartupTaskState.Enabled)
                    _logger.LogInformation("Autostart successfully activated.");
                else
                    _logger.LogWarning("Autostart activation failed (User denied?).");
            }
            else
            {
                _logger.LogInformation($"Autostart is already active or disabled by user. Status: {startupTask.State}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating autostart via StartupTask.");
        }
    }

    public async Task DisableAutoStartAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            startupTask.Disable();
            _logger.LogInformation("Autostart successfully deactivated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating autostart.");
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