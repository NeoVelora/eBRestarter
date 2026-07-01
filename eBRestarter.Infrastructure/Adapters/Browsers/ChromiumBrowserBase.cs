using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Browsers;

public abstract class ChromiumBrowserBase(IOsProcessControlOutboundPort processControlPort, ISettingsRepositoryOutboundPort settingsPort, IFileSystemOutboundPort fileSystemPort, ILogger logger) : BrowserBase(processControlPort, settingsPort, fileSystemPort, logger)
{
    protected abstract string ExeFileName { get; }
    protected abstract string BrowserRegistryName { get; }
    protected abstract string UninstallSubKey { get; }
    protected abstract string ProgramFilesSubPath { get; }

    protected abstract string ExtensionId { get; }

    protected abstract string[] UserDataSubPath { get; }

    protected override List<string> ExecutablePaths
    {
        get
        {
            var paths = new List<string>();

            var appPathKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{ExeFileName}";
            var appPath = _settingsPort.GetSystemValue(appPathKey, "")?.ToString();

            if (!string.IsNullOrEmpty(appPath)) { paths.Add(appPath); }

            var clientKey = $@"SOFTWARE\Clients\StartMenuInternet\{BrowserRegistryName}\shell\open\command";
            var clientPath = _settingsPort.GetSystemValue(clientKey, "")?.ToString();

            if (!string.IsNullOrEmpty(clientPath)) { paths.Add(clientPath.Replace("\"", "").Trim()); }

            AddPathFromUninstallKey(paths, $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, true);
            AddPathFromUninstallKey(paths, $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, true);
            AddPathFromUninstallKey(paths, $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, false);

            var pathPart = _fileSystemPort.CombinePaths(ProgramFilesSubPath, ExeFileName);

            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath("ProgramFiles"), pathPart));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath("ProgramFiles(x86)"), pathPart));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath("LocalAppData"), pathPart));

            return [.. paths.Distinct()];
        }
    }

    public override bool IsExtensionInstalled(string? extensionId = null)
    {
        var idToCheck = string.IsNullOrEmpty(extensionId) ? ExtensionId : extensionId;

        if (string.IsNullOrEmpty(idToCheck)) return false;

        var paths = ResolvePaths();

        if (paths.ExtensionsDirs == null || paths.ExtensionsDirs.Count == 0) return false;

        foreach (var extensionsDir in paths.ExtensionsDirs)
        {
            if (!_fileSystemPort.DirectoryExists(extensionsDir)) continue;


            if (extensionsDir.EndsWith(idToCheck, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var fullExtensionPath = _fileSystemPort.CombinePaths(extensionsDir, idToCheck);

            if (_fileSystemPort.DirectoryExists(fullExtensionPath))
            {
                return true;
            }
        }

        return false;
    }

    public override BrowserPaths ResolvePaths()
    {
        var localAppData = _fileSystemPort.ResolveEnvironmentPath("LocalAppData");

        var pathParts = new List<string> { localAppData };
        pathParts.AddRange(UserDataSubPath);
        var userDataRoot = _fileSystemPort.CombinePaths([.. pathParts]);

        var cacheDirs = new List<string>();
        var cookiesDirs = new List<string>();
        var extensionsDirs = new List<string>();

        var allProfileFolders = new List<string>();

        var defaultPath = _fileSystemPort.CombinePaths(userDataRoot, "Default");

        if (_fileSystemPort.DirectoryExists(defaultPath))
        {
            allProfileFolders.Add(defaultPath);
        }

        try
        {
            var dirs = _fileSystemPort.GetDirectories(userDataRoot, "Profile *");
            allProfileFolders.AddRange(dirs);
        }
        catch { }

        foreach (var profilePath in allProfileFolders)
        {
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, "Cache", "Cache_Data"));
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, "Service Worker"));
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, "Code Cache"));
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, "GPUCache"));

            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, "IndexedDB"));
            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, "Network"));
            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, "Local Storage"));
            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, "Session Storage"));

            extensionsDirs.Add(_fileSystemPort.CombinePaths(profilePath, "Extensions", ExtensionId));
        }

        return new BrowserPaths(cacheDirs, cookiesDirs, extensionsDirs);
    }
}

