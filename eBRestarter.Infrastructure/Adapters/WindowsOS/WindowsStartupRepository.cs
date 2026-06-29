using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.WindowsOS;

public sealed class WindowsStartupRepository(
    ILogger<WindowsStartupRepository> logger,
    ISettingsPort registry,
    IProcessInfoPort processInfo) : IAutoStartPort, IBrowserConfigPort
{
    private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";

    private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

    private readonly ILogger<WindowsStartupRepository> _logger = logger;
    private readonly ISettingsPort _registry = registry;
    private readonly IProcessInfoPort _processInfo = processInfo;

    public void EnableAutoStart()
    {
        try
        {
            string exePath = _processInfo.GetCurrentExecutablePath();
            _registry.SetUserValue(RegistryPathRun, "eBRestarter", exePath);
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
            _registry.DeleteUserValue(RegistryPathRun, "eBRestarter");
            _logger.LogInformation("Autostart entry for 'eBRestarter' removed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing autostart from registry.");
        }
    }

    public Dictionary<string, object> RetrieveStartupEntries()
    {
        try
        {
            return _registry.GetUserValues(RegistryPathRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving autostart entries.");

            return [];
        }
    }

    public void SetBrowserStartupBoost(bool enable)
    {
        try
        {
            int dwordValue = enable ? 1 : 0;

            _registry.SetSystemValue(RegistryPathEdgePolicies, "StartupBoostEnabled", dwordValue);

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

    public bool IsBrowserStartupBoostEnabled()
    {
        try
        {
            var value = _registry.GetSystemValue(RegistryPathEdgePolicies, "StartupBoostEnabled");

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
            _registry.SetSystemValue(RegistryPathPasswordLess, "DevicePasswordLessBuildVersion", dwordValue);

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
            var entries = RetrieveStartupEntries();
            bool isEnabled = entries.ContainsKey("eBRestarter");

            return Task.FromResult(isEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking autostart status.");
            return Task.FromResult(false);
        }
    }
}
