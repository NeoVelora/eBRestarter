using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.Browsers;

public abstract class AdapterBrowserBase(IOutboundPortOsProcessControl processControlPort, IOutboundPortSystemConfigurationRepository settingsPort, IOutboundPortFileSystem fileSystemPort, ILogger logger) : IOutboundPortBrowser
{
    protected readonly IOutboundPortOsProcessControl _processControlPort = processControlPort;
    protected readonly IOutboundPortSystemConfigurationRepository _settingsPort = settingsPort;
    protected readonly IOutboundPortFileSystem _fileSystemPort = fileSystemPort;
    protected readonly ILogger _logger = logger;

    public abstract BrowserType Type { get; }

    public abstract BrowserPaths ResolvePaths();

    public abstract bool IsExtensionInstalled(string? extensionId = null);
    public abstract string DisplayName { get; }
    public abstract string IconPath { get; }
    public abstract string DownloadUrl { get; }
    public abstract string ExtensionInstallUrl { get; }
    public abstract string ProcessName { get; }
    protected abstract string RegistryKeyVersion { get; }
    protected abstract List<string> ExecutablePaths { get; }

    public virtual string BrowserVersion
    {
        get
        {
            var raw = _settingsPort.GetUserValue(RegistryKeyVersion, "version")
                      ?? _settingsPort.GetUserValue(RegistryKeyVersion, "CurrentVersion");

            return CleanVersionString(raw?.ToString());
        }
    }

    public bool IsInstalled => ExecutablePaths.Any(path => _fileSystemPort.FileExists(path));

    public virtual void Start(string url, string arguments = "")
    {
        try
        {
            var exePath = RetrieveExecutablePath();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Starting {BrowserType} with URL {Url}...", Type, url);
            }

            var finalArguments = $"{url} {arguments}".Trim();

            _processControlPort.OpenUrlInBrowser(exePath, finalArguments);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error while starting {BrowserType}", Type);
            }
        }
    }

    public virtual void Close()
    {
        _processControlPort.CloseApplication(ProcessName);
    }

    protected string RetrieveExecutablePath()
    {
        return ExecutablePaths.FirstOrDefault(path => _fileSystemPort.FileExists(path))
               ?? throw new FileNotFoundException($"{Type} executable not found.");
    }

    protected void AddPathFromUninstallKey(List<string> paths, string subKey, string exeName, bool isHklm)
    {
        var val = isHklm
            ? _settingsPort.GetSystemValue(subKey, "InstallLocation")
            : _settingsPort.GetUserValue(subKey, "InstallLocation");

        if (val is not null && !string.IsNullOrEmpty(val.ToString()))
        {
            var fullPath = _fileSystemPort.CombinePaths(val.ToString()!, exeName);
            paths.Add(fullPath);
        }
    }

    protected virtual string CleanVersionString(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Unknown";

        var cleanRaw = raw.Trim();

        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                cleanRaw,
                @"^[\d\.]+",
                System.Text.RegularExpressions.RegexOptions.NonBacktracking,
                TimeSpan.FromMilliseconds(100));

            if (match.Success)
            {
                return match.Value;
            }
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            return "Unknown";
        }

        return raw;
    }
}


