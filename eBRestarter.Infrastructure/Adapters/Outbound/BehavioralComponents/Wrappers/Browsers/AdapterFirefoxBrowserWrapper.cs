using System.Globalization;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;

public sealed partial class AdapterFirefoxBrowserWrapper(
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortSystemConfigurationRepository settingsPort,
    IOutboundPortFileSystem fileSystemPort,
    ILogger<AdapterFirefoxBrowserWrapper> logger)
    : AdapterBrowserBaseWrapper(
        processControlPort,
        settingsPort,
        fileSystemPort,
        logger)
{

    private const string AppDataEnvVar = "AppData";
    private const string Cache2EntriesSubPath = @"cache2\entries";
    private const string DefaultDisplayName = "Firefox";
    private const string DefaultIconPath = "ms-appx:///Resources/Visuals/Icons/Intersection/fa_firefox.png";
    private const string DefaultProcessName = "firefox";
    private const string DefaultRegistryKeyVersion = @"Software\Mozilla\Mozilla Firefox";
    private const string EbesucherAddOnNameForFirefox = "{76e6445a-74a5-4c26-9afc-95dae514cb77}.xpi";
    private const string ExtensionsFolderName = "extensions";
    private const string FirefoxExeSubPath = @"Mozilla Firefox\firefox.exe";
    private const string FirefoxFolderName = "Firefox";
    private const string FirefoxRegistryCurrentVersionValueName = "CurrentVersion";
    private const string FirefoxRegistryMainSubKeyPattern = @"{0}\{1}\Main";
    private const string FirefoxRegistryPathToExeValueName = "PathToExe";
    private const string FirefoxRegistryRootKey = @"Software\Mozilla\Mozilla Firefox";
    private const string IniIsRelativeKeyPrefix = "IsRelative=";
    private const string IniPathKeyPrefix = "Path=";
    private const string LocalAppDataEnvVar = "LocalAppData";
    private const string MozillaFolderName = "Mozilla";
    private const string ProfileDefaultExtension = ".xpi";
    private const string ProfilesFolderName = "Profiles";
    private const string ProfilesIniFileName = "profiles.ini";
    private const string ProgramFilesEnvVar = "ProgramFiles";
    private const string ProgramFilesX86EnvVar = "ProgramFiles(x86)";
    private const string StartupCacheFolderName = "startupCache";
    private const string StorageDefaultSubPath = @"storage\default";


    public override string DisplayName => DefaultDisplayName;
    public override string DownloadUrl => WebLinks.FirefoxDownloadLink;
    public override string ExtensionInstallUrl => WebLinks.FirefoxEVisitorAddOnLink;
    public override string IconPath => DefaultIconPath;
    public override string ProcessName => DefaultProcessName;
    protected override string RegistryKeyVersion => DefaultRegistryKeyVersion;

    public override BrowserType Type => BrowserType.Firefox;

    protected override List<string> ExecutablePaths
    {
        get
        {
            var paths = new List<string>();

            var hkcuPath = RetrievePathFromMozillaRegistry(false);

            if (!string.IsNullOrEmpty(hkcuPath))
            {
                paths.Add(hkcuPath);
            }

            var hklmPath = RetrievePathFromMozillaRegistry(true);

            if (!string.IsNullOrEmpty(hklmPath))
            {
                paths.Add(hklmPath);
            }

            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath(ProgramFilesEnvVar), FirefoxExeSubPath));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath(ProgramFilesX86EnvVar), FirefoxExeSubPath));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath(LocalAppDataEnvVar), FirefoxExeSubPath));

            return [.. paths.Distinct(StringComparer.OrdinalIgnoreCase)];
        }
    }


    public override bool IsExtensionInstalled(string? extensionId = null)
    {
        var paths = ResolvePaths();

        if (paths.ExtensionsDirs is not { Count: > 0 })
        {
            return false;
        }

        var idToCheck = string.IsNullOrWhiteSpace(extensionId)
            ? EbesucherAddOnNameForFirefox.TrimStart('\\')
            : extensionId;

        foreach (var extensionsDir in paths.ExtensionsDirs)
        {
            if (!_fileSystemPort.DirectoryExists(extensionsDir))
            {
                continue;
            }

            var xpiPath = _fileSystemPort.CombinePaths(extensionsDir, idToCheck);

            if (_fileSystemPort.FileExists(xpiPath))
            {
                return true;
            }

            var folderName = idToCheck.Replace(ProfileDefaultExtension, string.Empty, StringComparison.OrdinalIgnoreCase);
            var folderPath = _fileSystemPort.CombinePaths(extensionsDir, folderName);

            if (_fileSystemPort.DirectoryExists(folderPath))
            {
                return true;
            }
        }

        return false;
    }

    public override BrowserPaths ResolvePaths()
    {
        var appData = _fileSystemPort.ResolveEnvironmentPath(AppDataEnvVar);
        var localAppData = _fileSystemPort.ResolveEnvironmentPath(LocalAppDataEnvVar);

        var firefoxRoamingRoot = _fileSystemPort.CombinePaths(appData, MozillaFolderName, FirefoxFolderName);
        var firefoxLocalRoot = _fileSystemPort.CombinePaths(localAppData, MozillaFolderName, FirefoxFolderName);

        List<string> cacheDirs = [];
        List<string> cookiesDirs = [];
        List<string> extensionsDirs = [];

        var profilesIniPath = _fileSystemPort.CombinePaths(firefoxRoamingRoot, ProfilesIniFileName);
        var profileInfos = RetrieveProfileFoldersFromIni(profilesIniPath);

        foreach (var profile in profileInfos)
        {
            string fullProfilePathRoaming;
            string fullProfilePathLocal;

            if (profile.IsRelative)
            {
                fullProfilePathRoaming = _fileSystemPort.CombinePaths(firefoxRoamingRoot, profile.Path);
                fullProfilePathLocal = _fileSystemPort.CombinePaths(firefoxLocalRoot, profile.Path);
            }
            else
            {
                fullProfilePathRoaming = profile.Path;
                var folderName = System.IO.Path.GetFileName(profile.Path);
                fullProfilePathLocal = _fileSystemPort.CombinePaths(firefoxLocalRoot, ProfilesFolderName, folderName);
            }

            cacheDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathLocal, Cache2EntriesSubPath));
            cacheDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathLocal, StartupCacheFolderName));

            cookiesDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathRoaming, StorageDefaultSubPath));

            extensionsDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathRoaming, ExtensionsFolderName));
        }

        return new BrowserPaths(cacheDirs, cookiesDirs, extensionsDirs);
    }

    private string? RetrievePathFromMozillaRegistry(bool isHklm)
    {
        var currentVersionObj = isHklm
            ? _settingsPort.GetSystemValue(FirefoxRegistryRootKey, FirefoxRegistryCurrentVersionValueName)
            : _settingsPort.GetUserValue(FirefoxRegistryRootKey, FirefoxRegistryCurrentVersionValueName);

        if (currentVersionObj is null)
        {
            return null;
        }

        var currentVersion = currentVersionObj.ToString()!;
        var mainKeyPath = string.Format(CultureInfo.InvariantCulture, FirefoxRegistryMainSubKeyPattern, FirefoxRegistryRootKey, currentVersion);

        var pathToExeObj = isHklm
            ? _settingsPort.GetSystemValue(mainKeyPath, FirefoxRegistryPathToExeValueName)
            : _settingsPort.GetUserValue(mainKeyPath, FirefoxRegistryPathToExeValueName);

        return pathToExeObj?.ToString();
    }

    private List<ProfileInfo> RetrieveProfileFoldersFromIni(string iniPath)
    {
        List<ProfileInfo> profiles = [];

        if (!_fileSystemPort.FileExists(iniPath))
        {
            return profiles;
        }

        try
        {
            var lines = _fileSystemPort.ReadAllLines(iniPath);

            string? currentPath = null;
            bool? currentIsRelative = null;

            foreach (var line in lines)
            {
                var trimmed = line.AsSpan().Trim();

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    if (currentPath is not null)
                    {
                        profiles.Add(new ProfileInfo(currentPath.Replace('/', '\\'), currentIsRelative ?? true));
                    }

                    currentPath = null;
                    currentIsRelative = null;
                    continue;
                }

                if (trimmed.StartsWith(IniPathKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    currentPath = trimmed[IniPathKeyPrefix.Length..].Trim().ToString();
                }
                else if (trimmed.StartsWith(IniIsRelativeKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    currentIsRelative = trimmed.EndsWith("1", StringComparison.Ordinal);
                }
            }

            if (currentPath is not null)
            {
                profiles.Add(new ProfileInfo(currentPath.Replace('/', '\\'), currentIsRelative ?? true));
            }
        }
        catch (Exception exception)
        {
            LogProfileFileParsingFailed(_logger, exception, iniPath);
        }

        return profiles;
    }


    [LoggerMessage(
        EventId = LogEventIds.Browser.BrowserProfileFileUnreadable,
        Level = LogLevel.Error,
        Message = "Error while parsing profiles.ini at {IniPath}")]
    private static partial void LogProfileFileParsingFailed(ILogger logger, Exception exception, string iniPath);


    private readonly record struct ProfileInfo(string Path, bool IsRelative);
}
