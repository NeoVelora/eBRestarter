using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Versioning;
using System.Security.Principal;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS;

[SupportedOSPlatform("windows")]
public sealed class AdapterWindowsSystemInfoProvider : IOutboundPortSystemInfoProvider
{
    private const string BrowserNameBrave = "Brave";
    private const string BrowserNameChrome = "Chrome";
    private const string BrowserNameEdge = "Edge";
    private const string BrowserNameFirefox = "Firefox";
    private const string BrowserNameOpera = "Opera";
    private const string DefaultBrowserFallbackText = "-";
    private const string DifferentBrowsersDetectedLogMessage = "Different browsers detected for HTTP ({Http}) and HTTPS ({Https}).";
    private const string DisplayVersionValueName = "DisplayVersion";
    private const string ErrorReadingOsBuildVersionLogMessage = "Error reading the OS build version.";
    private const string ErrorReadingOsDisplayVersionLogMessage = "Error reading the OS display version.";
    private const string ErrorVersionFallback = "Error";
    private const string ProgIdKeywordBrave = "Brave";
    private const string ProgIdKeywordChrome = "ChromeHTML";
    private const string ProgIdKeywordEdge = "MSEdge";
    private const string ProgIdKeywordFirefox = "Firefox";
    private const string ProgIdKeywordOpera = "Opera";
    private const string ProgIdValueName = "ProgId";
    private const string RegistryPathCurrentVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string RegistryPathUserChoiceHttp = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
    private const string RegistryPathUserChoiceHttps = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";
    private const string UbrValueName = "UBR";
    private const string UnableToReadRegistryPathLogMessage = "Unable to read the registry path: {Path}";
    private const string UnknownVersionFallback = "Unknown";

    private readonly ILogger<AdapterWindowsSystemInfoProvider> _logger;
    private readonly IOutboundPortSystemConfigurationRepository _registry;


    public AdapterWindowsSystemInfoProvider(
        ILogger<AdapterWindowsSystemInfoProvider> logger,
        IOutboundPortSystemConfigurationRepository registry)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(registry);

        _logger = logger;
        _registry = registry;
    }


    public bool IsUserAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public string RetrieveCurrentOsBuildVersion()
    {
        try
        {
            var ubrObj = _registry.GetSystemValue(RegistryPathCurrentVersion, UbrValueName);

            string ubr = ubrObj?.ToString() ?? "0";

            return $"{Environment.OSVersion.Version.Build}.{ubr}";
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventIds.OperatingSystem.OsVersionReadFailed, ex, ErrorReadingOsBuildVersionLogMessage);

            return ErrorVersionFallback;
        }
    }

    public string RetrieveCurrentOsDisplayVersion()
    {
        try
        {
            var versionObj = _registry.GetSystemValue(RegistryPathCurrentVersion, DisplayVersionValueName);

            var version = versionObj?.ToString();

            return !string.IsNullOrWhiteSpace(version) ? version : UnknownVersionFallback;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventIds.OperatingSystem.OsVersionReadFailed, ex, ErrorReadingOsDisplayVersionLogMessage);

            return ErrorVersionFallback;
        }
    }

    public string RetrieveCurrentStandardBrowserName()
    {
        string? progIdHttp = GetRegistryValueAsString(RegistryPathUserChoiceHttp, ProgIdValueName);
        string? progIdHttps = GetRegistryValueAsString(RegistryPathUserChoiceHttps, ProgIdValueName);

        if (string.IsNullOrEmpty(progIdHttp) || string.IsNullOrEmpty(progIdHttps))
        {
            return DefaultBrowserFallbackText;
        }

        if (!string.Equals(progIdHttp, progIdHttps, StringComparison.OrdinalIgnoreCase) && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogDebug(LogEventIds.OperatingSystem.DefaultBrowserMismatch, DifferentBrowsersDetectedLogMessage, progIdHttp, progIdHttps);
        }

        return IdentifyBrowserByProgId(progIdHttp);
    }

    private string? GetRegistryValueAsString(string subKey, string valueName)
    {
        try
        {
            return _registry.GetUserValue(subKey, valueName)?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(LogEventIds.OperatingSystem.RegistryReadFailed, ex, UnableToReadRegistryPathLogMessage, subKey);
            return null;
        }
    }

    private static string IdentifyBrowserByProgId(string progId)
    {
        return progId switch
        {
            string p when p.Contains(ProgIdKeywordChrome, StringComparison.OrdinalIgnoreCase) => BrowserNameChrome,
            string p when p.Contains(ProgIdKeywordFirefox, StringComparison.OrdinalIgnoreCase) => BrowserNameFirefox,
            string p when p.Contains(ProgIdKeywordEdge, StringComparison.OrdinalIgnoreCase) => BrowserNameEdge,
            string p when p.Contains(ProgIdKeywordOpera, StringComparison.OrdinalIgnoreCase) => BrowserNameOpera,
            string p when p.Contains(ProgIdKeywordBrave, StringComparison.OrdinalIgnoreCase) => BrowserNameBrave,
            _ => DefaultBrowserFallbackText
        };
    }
}
