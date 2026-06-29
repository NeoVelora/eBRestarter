using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Browser;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Browsers;

public sealed class FirefoxBrowser(IOsProcessControlPort processControlPort, ISettingsPort settingsPort, IFileSystemPort fileSystemPort, ILogger<FirefoxBrowser> logger) : BrowserBase(processControlPort, settingsPort, fileSystemPort, logger)
{
    private const string EbesucherAddOnNameForFirefox = "{76e6445a-74a5-4c26-9afc-95dae514cb77}.xpi";

    public override string DisplayName => "Firefox";
    public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_firefox.png";
    public override string DownloadUrl => WebLinks.FirefoxDownloadLink;
    public override string ExtensionInstallUrl => WebLinks.FirefoxEVisitorAddOnLink;
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

            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath("ProgramFiles"), @"Mozilla Firefox\firefox.exe"));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath("ProgramFiles(x86)"), @"Mozilla Firefox\firefox.exe"));
            paths.Add(_fileSystemPort.CombinePaths(_fileSystemPort.ResolveEnvironmentPath("LocalAppData"), @"Mozilla Firefox\firefox.exe"));

            return [.. paths.Distinct()];

            string? RetrievePathFromMozillaRegistry(bool isHklm)
            {
                const string rootKey = @"Software\Mozilla\Mozilla Firefox";

                var currentVersionObj = isHklm
                    ? _settingsPort.GetSystemValue(rootKey, "CurrentVersion")
                    : _settingsPort.GetUserValue(rootKey, "CurrentVersion");

                if (currentVersionObj is null) return null;

                string currentVersion = currentVersionObj.ToString()!;
                string mainKeyPath = $@"{rootKey}\{currentVersion}\Main";

                var pathToExeObj = isHklm
                    ? _settingsPort.GetSystemValue(mainKeyPath, "PathToExe")
                    : _settingsPort.GetUserValue(mainKeyPath, "PathToExe");

                return pathToExeObj?.ToString();
            }
        }
    }

    public override string ProcessName => "firefox";
    protected override string RegistryKeyVersion => @"Software\Mozilla\Mozilla Firefox";

    public override BrowserPaths ResolvePaths()
    {
        var appData = _fileSystemPort.ResolveEnvironmentPath("AppData");
        var localAppData = _fileSystemPort.ResolveEnvironmentPath("LocalAppData");

        var firefoxRoamingRoot = _fileSystemPort.CombinePaths(appData, "Mozilla", "Firefox");
        var firefoxLocalRoot = _fileSystemPort.CombinePaths(localAppData, "Mozilla", "Firefox");

        var cacheDirs = new List<string>();
        var cookiesDirs = new List<string>();
        var extensionsDirs = new List<string>();

        var profilesIniPath = _fileSystemPort.CombinePaths(firefoxRoamingRoot, "profiles.ini");
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

                var folderName = new DirectoryInfo(profile.Path).Name;
                fullProfilePathLocal = _fileSystemPort.CombinePaths(firefoxLocalRoot, "Profiles", folderName);
            }

            cacheDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathLocal, "cache2", "entries"));

            cacheDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathLocal, "startupCache"));

            cookiesDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathRoaming, "storage", "default"));

            extensionsDirs.Add(_fileSystemPort.CombinePaths(fullProfilePathRoaming, "extensions"));
        }

        return new BrowserPaths(cacheDirs, cookiesDirs, extensionsDirs);
    }

    public override bool IsExtensionInstalled(string? extensionId = null)
    {
        var paths = ResolvePaths();

        if (paths.ExtensionsDirs is null || paths.ExtensionsDirs.Count == 0) return false;

        var idToCheck = string.IsNullOrEmpty(extensionId)
            ? EbesucherAddOnNameForFirefox.TrimStart('\\')
            : extensionId;

        foreach (var extensionsDir in paths.ExtensionsDirs)
        {
            if (!_fileSystemPort.DirectoryExists(extensionsDir)) continue;

            var xpiPath = _fileSystemPort.CombinePaths(extensionsDir, idToCheck);
            if (_fileSystemPort.FileExists(xpiPath)) return true;

            var folderName = idToCheck.Replace(".xpi", "", StringComparison.OrdinalIgnoreCase);
            var folderPath = _fileSystemPort.CombinePaths(extensionsDir, folderName);
            if (_fileSystemPort.DirectoryExists(folderPath)) return true;
        }

        return false;
    }

    private List<ProfileInfo> RetrieveProfileFoldersFromIni(string iniPath)
    {
        var profiles = new List<ProfileInfo>();

        if (!_fileSystemPort.FileExists(iniPath)) return profiles;

        try
        {
            var lines = _fileSystemPort.ReadAllLines(iniPath);

            string? currentPath = null;
            bool? currentIsRelative = null;

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith('[') && line.Trim().EndsWith(']'))
                {
                    if (currentPath is not null)
                    {
                        profiles.Add(new ProfileInfo
                        {
                            Path = currentPath.Replace('/', '\\'),
                            IsRelative = currentIsRelative ?? true
                        });
                    }

                    currentPath = null;
                    currentIsRelative = null;
                    continue;
                }

                if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                {
                    currentPath = line[5..].Trim();
                }
                else if (line.StartsWith("IsRelative=", StringComparison.OrdinalIgnoreCase))
                {
                    currentIsRelative = line.Trim().EndsWith('1');
                }
            }

            if (currentPath is not null)
            {
                profiles.Add(new ProfileInfo
                {
                    Path = currentPath.Replace('/', '\\'),
                    IsRelative = currentIsRelative ?? true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing profiles.ini");
        }

        return profiles;
    }

    private sealed class ProfileInfo
    {
        public string Path { get; set; } = string.Empty;
        public bool IsRelative { get; set; } = true;
    }
}


