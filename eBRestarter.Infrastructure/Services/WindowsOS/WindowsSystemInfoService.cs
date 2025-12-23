using eBRestarter.Application.Services.Ports.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS
{
    [SupportedOSPlatform("windows")]
    public class WindowsSystemInfoService : ISystemInfoService
    {
        private const string RegistryPathUserChoiceHttp = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
        private const string RegistryPathUserChoiceHttps = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";
        private const string RegistryPathCurrentVersion = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        private readonly ILogger<WindowsSystemInfoService> _logger;

        public WindowsSystemInfoService(ILogger<WindowsSystemInfoService> logger)
        {
            _logger = logger;
        }

        public string GetCurrentOsDisplayVersion()
        {
            try
            {
                var version = Registry.GetValue(RegistryPathCurrentVersion, "DisplayVersion", "")?.ToString();
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
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                if (key == null) return "Unknown";

                var ubr = key.GetValue("UBR")?.ToString() ?? "0";
                return $"{Environment.OSVersion.Version.Build}.{ubr}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Lesen der OS Build Version.");
                return "Error";
            }
        }

        private string? GetRegistryValueAsString(string keyPath, string valueName)
        {
            try
            {
                return Registry.GetValue(keyPath, valueName, null) as string;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Konnte Registry-Pfad nicht lesen: {Path}", keyPath);

                return null;
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

        private static string IdentifyBrowserByProgId(string progId)
        {
            if (progId.Contains("ChromeHTML", StringComparison.OrdinalIgnoreCase))
            {
                return "Chrome";
            }

            if (progId.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
            {
                return "Firefox";
            }

            if (progId.Contains("MSEdge", StringComparison.OrdinalIgnoreCase))
            {
                return "Edge";
            }

            if (progId.Contains("Opera", StringComparison.OrdinalIgnoreCase))
            {
                return "Opera";
            }

            if (progId.Contains("Brave", StringComparison.OrdinalIgnoreCase))
            {
                return "Brave";
            }

            return "-";
        }
    }
}
