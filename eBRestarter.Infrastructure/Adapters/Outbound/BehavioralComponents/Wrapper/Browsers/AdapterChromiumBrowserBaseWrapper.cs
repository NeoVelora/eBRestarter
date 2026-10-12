using System.Globalization;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;

public abstract class AdapterChromiumBrowserBaseWrapper(
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortSystemConfigurationRepository settingsPort,
    IOutboundPortFileSystem fileSystemPort,
    ILogger logger)
    : AdapterBrowserBaseWrapper(
        processControlPort,
        settingsPort,
        fileSystemPort,
        logger)
{

    private const string AppPathsRegistryKeyPattern = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{0}";
    private const string CacheDataSubPath = "Cache_Data";
    private const string CacheSubPath = "Cache";
    private const string CodeCacheSubPath = "Code Cache";
    private const string DefaultProfileFolderName = "Default";
    private const string ExtensionsFolderName = "Extensions";
    private const string GpuCacheSubPath = "GPUCache";
    private const string IndexedDbSubPath = "IndexedDB";
    private const string LocalAppDataEnvVar = "LocalAppData";
    private const string LocalStorageSubPath = "Local Storage";
    private const string MachineUninstallRegistryKeyPattern = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}";
    private const string NetworkSubPath = "Network";
    private const string ProfileFolderSearchPattern = "Profile *";
    private const string ProgramFilesEnvVar = "ProgramFiles";
    private const string ProgramFilesX86EnvVar = "ProgramFiles(x86)";
    private const string ServiceWorkerSubPath = "Service Worker";
    private const string SessionStorageSubPath = "Session Storage";
    private const string StartMenuInternetRegistryKeyPattern = @"SOFTWARE\Clients\StartMenuInternet\{0}\shell\open\command";
    private const string UserUninstallRegistryKeyPattern = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{0}";
    private const string Wow64UninstallRegistryKeyPattern = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{0}";


    protected abstract string BrowserRegistryName { get; }
    protected abstract string ExeFileName { get; }
    protected abstract string ExtensionId { get; }
    protected abstract string ProgramFilesSubPath { get; }
    protected abstract string UninstallSubKey { get; }

    protected override List<string> ExecutablePaths
    {
        get
        {
            var paths = new List<string>();

            var appPathKey = string.Format(CultureInfo.InvariantCulture, AppPathsRegistryKeyPattern, ExeFileName);
            var appPath = _settingsPort.GetSystemValue(appPathKey, string.Empty)?.ToString();

            if (!string.IsNullOrEmpty(appPath))
            {
                paths.Add(appPath);
            }

            var clientKey = string.Format(CultureInfo.InvariantCulture, StartMenuInternetRegistryKeyPattern, BrowserRegistryName);
            var clientPath = _settingsPort.GetSystemValue(clientKey, string.Empty)?.ToString();

            if (!string.IsNullOrEmpty(clientPath))
            {
                paths.Add(clientPath.Replace("\"", string.Empty, StringComparison.Ordinal).Trim());
            }

            AddPathFromUninstallKey(paths, string.Format(CultureInfo.InvariantCulture, MachineUninstallRegistryKeyPattern, UninstallSubKey), ExeFileName, true);
            AddPathFromUninstallKey(paths, string.Format(CultureInfo.InvariantCulture, Wow64UninstallRegistryKeyPattern, UninstallSubKey), ExeFileName, true);
            AddPathFromUninstallKey(paths, string.Format(CultureInfo.InvariantCulture, UserUninstallRegistryKeyPattern, UninstallSubKey), ExeFileName, false);

            var pathPart = _fileSystemPort.CombinePaths(ProgramFilesSubPath, ExeFileName);

            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath(ProgramFilesEnvVar), pathPart));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath(ProgramFilesX86EnvVar), pathPart));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath(LocalAppDataEnvVar), pathPart));

            return [.. paths.Distinct(StringComparer.OrdinalIgnoreCase)];
        }
    }

    protected abstract string[] UserDataSubPath { get; }


    public override bool IsExtensionInstalled(string? extensionId = null)
    {
        var idToCheck = string.IsNullOrWhiteSpace(extensionId) ? ExtensionId : extensionId;

        if (string.IsNullOrWhiteSpace(idToCheck))
        {
            return false;
        }

        var paths = ResolvePaths();

        if (paths.ExtensionsDirs is not { Count: > 0 })
        {
            return false;
        }

        foreach (var extensionsDir in paths.ExtensionsDirs)
        {
            if (!_fileSystemPort.DirectoryExists(extensionsDir))
            {
                continue;
            }

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
        var localAppData = _fileSystemPort.ResolveEnvironmentPath(LocalAppDataEnvVar);
        var userDataRoot = _fileSystemPort.CombinePaths([localAppData, .. UserDataSubPath]);

        List<string> cacheDirs = [];
        List<string> cookiesDirs = [];
        List<string> extensionsDirs = [];
        List<string> allProfileFolders = [];

        var defaultPath = _fileSystemPort.CombinePaths(userDataRoot, DefaultProfileFolderName);

        if (_fileSystemPort.DirectoryExists(defaultPath))
        {
            allProfileFolders.Add(defaultPath);
        }

        try
        {
            var dirs = _fileSystemPort.GetDirectories(userDataRoot, ProfileFolderSearchPattern);
            allProfileFolders.AddRange(dirs);
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(LogEventIds.Browser.BrowserProfileDiscoveryFailed, exception, "Could not retrieve additional profile folders for Chromium user data path {UserDataRoot}", userDataRoot);
            }
        }

        foreach (var profilePath in allProfileFolders)
        {
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, CacheSubPath, CacheDataSubPath));
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, ServiceWorkerSubPath));
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, CodeCacheSubPath));
            cacheDirs.Add(_fileSystemPort.CombinePaths(profilePath, GpuCacheSubPath));

            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, IndexedDbSubPath));
            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, NetworkSubPath));
            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, LocalStorageSubPath));
            cookiesDirs.Add(_fileSystemPort.CombinePaths(profilePath, SessionStorageSubPath));

            extensionsDirs.Add(_fileSystemPort.CombinePaths(profilePath, ExtensionsFolderName, ExtensionId));
        }

        return new BrowserPaths(cacheDirs, cookiesDirs, extensionsDirs);
    }
}
