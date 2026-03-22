using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

[SupportedOSPlatform("windows")]
public class WindowsSystemInfoService(ILogger<WindowsSystemInfoService> logger, IWindowsRegistryService registry) : IWindowsSystemInfoService
{
    private const string RegistryPathUserChoiceHttp = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
    private const string RegistryPathUserChoiceHttps = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";
    private const string RegistryPathCurrentVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    private readonly ILogger<WindowsSystemInfoService> _logger = logger;
    private readonly IWindowsRegistryService _registry = registry;

    public string GetCurrentOsDisplayVersion()
    {
        try
        {
            var versionObj = _registry.GetLocalMachineValue(RegistryPathCurrentVersion, "DisplayVersion");

            var version = versionObj?.ToString();

            return !string.IsNullOrWhiteSpace(version) ? version : "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Lesen der OS Display Version.");
            return "Error";
        }
    }

    public string GetCurrentOsBuildVersion()
    {
        try
        {
            var ubrObj = _registry.GetLocalMachineValue(RegistryPathCurrentVersion, "UBR");

            string ubr = ubrObj?.ToString() ?? "0";

            return $"{Environment.OSVersion.Version.Build}.{ubr}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Lesen der OS Build Version.");
            return "Error";
        }
    }

    public string GetCurrentStandardBrowserName()
    {
        string? progIdHttp = GetRegistryValueAsString(RegistryPathUserChoiceHttp, "ProgId");
        string? progIdHttps = GetRegistryValueAsString(RegistryPathUserChoiceHttps, "ProgId");

        if (string.IsNullOrEmpty(progIdHttp) || string.IsNullOrEmpty(progIdHttps))
        {
            return "-";
        }

        if (!string.Equals(progIdHttp, progIdHttps, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Unterschiedliche Browser für HTTP ({Http}) und HTTPS ({Https}) erkannt.", progIdHttp, progIdHttps);
        }

        return IdentifyBrowserByProgId(progIdHttp);
    }

    private string? GetRegistryValueAsString(string subKey, string valueName)
    {
        try
        {
            return _registry.GetCurrentUserValue(subKey, valueName)?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Konnte Registry-Pfad nicht lesen: {Path}", subKey);
            return null;
        }
    }

    private static string IdentifyBrowserByProgId(string progId)
    {
        if (progId.Contains("ChromeHTML", StringComparison.OrdinalIgnoreCase)) return "Chrome";
        if (progId.Contains("Firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox";
        if (progId.Contains("MSEdge", StringComparison.OrdinalIgnoreCase)) return "Edge";
        if (progId.Contains("Opera", StringComparison.OrdinalIgnoreCase)) return "Opera";
        if (progId.Contains("Brave", StringComparison.OrdinalIgnoreCase)) return "Brave";

        return "-";
    }
}
