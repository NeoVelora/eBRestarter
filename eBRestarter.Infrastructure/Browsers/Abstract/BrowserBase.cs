using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers.Abstract;

public abstract class BrowserBase(IOperatingSystemFacade os, ILogger logger) : IBrowser
{
    protected readonly IOperatingSystemFacade _os = os;
    protected readonly ILogger _logger = logger;

    public abstract BrowserType Type { get; }

    public abstract string DisplayName { get; }
    public abstract string IconPath { get; }
    public abstract string DownloadUrl { get; }

    public abstract string ExtensionInstallUrl { get; }
    protected abstract string ProcessName { get; }
    protected abstract string RegistryKeyVersion { get; }
    protected abstract List<string> ExecutablePaths { get; }

    public abstract bool IsExtensionInstalled(string? extensionId = null);

    public virtual void Start(string url, string arguments = "")
    {
        try
        {
            var exePath = GetExecutablePath();
            _logger.LogInformation($"Starte {Type} mit URL {url}...");

            string finalArguments = $"{url} {arguments}".Trim();

            _os.WindowsProcessControlService.OpenUrlInBrowser(exePath, finalArguments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Fehler beim Starten von {Type}");
        }
    }

    public virtual void Close()
    {
        _os.WindowsProcessControlService.CloseApplication(ProcessName);
    }

    public virtual string BrowserVersion
    {
        get
        {
            var raw = _os.WindowsRegistryService.GetCurrentUserValue(RegistryKeyVersion, "version") ?? _os.WindowsRegistryService.GetCurrentUserValue(RegistryKeyVersion, "CurrentVersion");

            return CleanVersionString(raw?.ToString());
        }
    }

    public bool IsInstalled => ExecutablePaths.Any(path => _os.WindowsFileSystemService.FileExists(path));

    protected string GetExecutablePath()
    {
        return ExecutablePaths.FirstOrDefault(path => _os.WindowsFileSystemService.FileExists(path)) ?? throw new FileNotFoundException($"{Type} executable not found.");
    }

    public abstract BrowserPaths GetPaths();

    protected void AddPathFromUninstallKey(List<string> paths, string subKey, string exeName, bool isHklm)
    {
        var val = isHklm
            ? _os.WindowsRegistryService.GetLocalMachineValue(subKey, "InstallLocation")
            : _os.WindowsRegistryService.GetCurrentUserValue(subKey, "InstallLocation");

        if (val != null && !string.IsNullOrEmpty(val.ToString()))
        {
            var fullPath = _os.WindowsFileSystemService.CombinePaths(val.ToString()!, exeName);
            paths.Add(fullPath);
        }
    }

    protected virtual string CleanVersionString(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Unknown";

        var cleanRaw = raw.Trim();

        var match = System.Text.RegularExpressions.Regex.Match(cleanRaw, @"^[\d\.]+");

        if (match.Success)
        {
            return match.Value;
        }

        return raw;
    }
}
