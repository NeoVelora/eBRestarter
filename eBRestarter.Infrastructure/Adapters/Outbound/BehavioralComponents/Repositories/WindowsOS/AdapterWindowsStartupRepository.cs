using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.WindowsOS;

public sealed class AdapterWindowsStartupRepository : IOutboundPortAutoStartRepository, IOutboundPortBrowserConfigRepository
{
    private const string AccessDeniedEdgePolicyExceptionMessage = "Access denied when changing registry settings. Please run as Administrator.";
    private const string AccessDeniedPasswordLessExceptionMessage = "Access denied when changing PasswordLess settings. Please run as Administrator.";
    private const int ActiveDwordValue = 1;
    private const string ActiveStateText = "Active";
    private const string ApplicationAutostartKey = "eBRestarter";
    private const string AutoLogonSettingLogMessage = "AutoLogon setting set to {State} (RegValue: {Value}).";
    private const string AutostartCreatedLogMessage = "Autostart entry for 'eBRestarter' successfully created.";
    private const string AutostartRemovedLogMessage = "Autostart entry for 'eBRestarter' removed.";
    private const string CouldNotReadEdgePolicyLogMessage = "Could not read Edge Startup Boost status.";
    private const string DevicePasswordLessBuildVersionValueName = "DevicePasswordLessBuildVersion";
    private const int DisabledDwordValue = 0;
    private const string EdgeStartupBoostEnabledValueName = "StartupBoostEnabled";
    private const string EdgeStartupBoostLogMessage = "Edge Startup Boost set to {State} (Value: {Value}).";
    private const string ErrorCheckingAutostartStatusLogMessage = "Error while checking autostart status.";
    private const string ErrorRemovingAutostartLogMessage = "Error removing autostart from registry.";
    private const string ErrorRetrievingAutostartEntriesLogMessage = "Error retrieving autostart entries.";
    private const string ErrorSettingAutostartLogMessage = "Error setting autostart in registry.";
    private const string FailedEdgePolicyExceptionMessage = "Failed to set Edge Startup Boost policy.";
    private const string FailedPasswordLessExceptionMessage = "Failed to configure PasswordLess AutoLogon registry setting.";
    private const int InactiveDwordValue = 2;
    private const string InactiveStateText = "Inactive";
    private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";
    private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";
    private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private readonly ILogger<AdapterWindowsStartupRepository> _logger;
    private readonly IOutboundPortProcessInfoProvider _processInfo;
    private readonly IOutboundPortSystemConfigurationRepository _registry;


    public AdapterWindowsStartupRepository(
        ILogger<AdapterWindowsStartupRepository> logger,
        IOutboundPortProcessInfoProvider processInfo,
        IOutboundPortSystemConfigurationRepository registry)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(processInfo);
        ArgumentNullException.ThrowIfNull(registry);

        _logger = logger;
        _processInfo = processInfo;
        _registry = registry;
    }


    public void DisableAutoStart()
    {
        try
        {
            _registry.DeleteUserValue(RegistryPathRun, ApplicationAutostartKey);
            _logger.LogInformation(LogEventIds.OperatingSystem.AutoStartDisabled, AutostartRemovedLogMessage);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(ErrorRemovingAutostartLogMessage, exception);
        }
    }

    public Task DisableAutoStartAsync()
    {
        DisableAutoStart();
        return Task.CompletedTask;
    }

    public void EnableAutoStart()
    {
        try
        {
            string exePath = _processInfo.GetCurrentExecutablePath();
            _registry.SetUserValue(RegistryPathRun, ApplicationAutostartKey, $"\"{exePath}\"");
            _logger.LogInformation(LogEventIds.OperatingSystem.AutoStartEnabled, AutostartCreatedLogMessage);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(ErrorSettingAutostartLogMessage, exception);
        }
    }

    public Task EnableAutoStartAsync()
    {
        EnableAutoStart();
        return Task.CompletedTask;
    }

    public Task<bool> IsAutoStartEnabledAsync()
    {
        try
        {
            var entries = RetrieveStartupEntries();
            bool isEnabled = entries.ContainsKey(ApplicationAutostartKey);
            return Task.FromResult(isEnabled);
        }
        catch (Exception exception)
        {
            _logger.LogError(LogEventIds.OperatingSystem.AutoStartStatusReadFailed, exception, ErrorCheckingAutostartStatusLogMessage);
            return Task.FromResult(false);
        }
    }

    public bool IsBrowserStartupBoostEnabled()
    {
        try
        {
            var value = _registry.GetSystemValue(RegistryPathEdgePolicies, EdgeStartupBoostEnabledValueName);

            if (value is ActiveDwordValue)
            {
                return true;
            }

            if (value is DisabledDwordValue)
            {
                return false;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(LogEventIds.OperatingSystem.EdgeStartupBoostReadFailed, exception, CouldNotReadEdgePolicyLogMessage);
        }

        return true;
    }

    public Dictionary<string, object> RetrieveStartupEntries()
    {
        try
        {
            return _registry.GetUserValues(RegistryPathRun);
        }
        catch (Exception exception)
        {
            _logger.LogError(LogEventIds.OperatingSystem.AutoStartStatusReadFailed, exception, ErrorRetrievingAutostartEntriesLogMessage);
            return [];
        }
    }

    public void SetAutoLogon(bool enable)
    {
        try
        {
            int dwordValue = enable ? DisabledDwordValue : InactiveDwordValue;
            _registry.SetSystemValue(RegistryPathPasswordLess, DevicePasswordLessBuildVersionValueName, dwordValue);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(LogEventIds.Security.PasswordlessModeChanged, AutoLogonSettingLogMessage, enable ? ActiveStateText : InactiveStateText, dwordValue);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new UnauthorizedAccessException(AccessDeniedPasswordLessExceptionMessage, exception);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(FailedPasswordLessExceptionMessage, exception);
        }
    }

    public void SetBrowserStartupBoost(bool enable)
    {
        try
        {
            int dwordValue = enable ? ActiveDwordValue : DisabledDwordValue;

            _registry.SetSystemValue(RegistryPathEdgePolicies, EdgeStartupBoostEnabledValueName, dwordValue);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(LogEventIds.OperatingSystem.EdgeStartupBoostChanged, EdgeStartupBoostLogMessage, enable ? ActiveStateText : InactiveStateText, dwordValue);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new UnauthorizedAccessException(AccessDeniedEdgePolicyExceptionMessage, exception);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(FailedEdgePolicyExceptionMessage, exception);
        }
    }
}
