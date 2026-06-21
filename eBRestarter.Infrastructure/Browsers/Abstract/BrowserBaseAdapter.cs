using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers.Abstract;

public abstract class BrowserBaseAdapter(IOperatingSystemFacade os, ILogger logger) : IBrowser
{
    protected readonly IOperatingSystemFacade _os = os;
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
            var raw = _os.WindowsRegistryAdapter.RetrieveCurrentUserValue(RegistryKeyVersion, "version") ?? _os.WindowsRegistryAdapter.RetrieveCurrentUserValue(RegistryKeyVersion, "CurrentVersion");

            return CleanVersionString(raw?.ToString());
        }
    }

    public bool IsInstalled => ExecutablePaths.Any(path => _os.WindowsFileSystemServiceAdapter.FileExists(path));


    public virtual void Start(string url, string arguments = "")
    {
        try
        {
            var exePath = RetrieveExecutablePath();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Starte {BrowserType} mit URL {Url}...", Type, url);
            }

            var finalArguments = $"{url} {arguments}".Trim();

            _os.WindowsProcessControlService.OpenUrlInBrowser(exePath, finalArguments);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Fehler beim Starten von {BrowserType}", Type);
            }
        }
    }

    public virtual void Close()
    {
        _os.WindowsProcessControlService.CloseApplication(ProcessName);
    }

    protected string RetrieveExecutablePath()
    {
        return ExecutablePaths.FirstOrDefault(path => _os.WindowsFileSystemServiceAdapter.FileExists(path)) ?? throw new FileNotFoundException($"{Type} executable not found.");
    }

    protected void AddPathFromUninstallKey(List<string> paths, string subKey, string exeName, bool isHklm)
    {
        var val = isHklm
            ? _os.WindowsRegistryAdapter.RetrieveLocalMachineValue(subKey, "InstallLocation")
            : _os.WindowsRegistryAdapter.RetrieveCurrentUserValue(subKey, "InstallLocation");

        if (val is not null && !string.IsNullOrEmpty(val.ToString()))
        {
            var fullPath = _os.WindowsFileSystemServiceAdapter.CombinePaths(val.ToString()!, exeName);
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


