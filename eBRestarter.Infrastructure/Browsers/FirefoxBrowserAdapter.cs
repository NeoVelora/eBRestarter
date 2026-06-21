using eBRestarter.Core.Application.Constants;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers;

public class FirefoxBrowserAdapter(IOperatingSystemFacade os, ILogger<FirefoxBrowserAdapter> logger) : BrowserBaseAdapter(os, logger)
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

            paths.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(_os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("ProgramFiles"), @"Mozilla Firefox\firefox.exe"));
            paths.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(_os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("ProgramFiles(x86)"), @"Mozilla Firefox\firefox.exe"));
            paths.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(_os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("LocalAppData"), @"Mozilla Firefox\firefox.exe"));

            return [.. paths.Distinct()];

            string? RetrievePathFromMozillaRegistry(bool isHklm)
            {
                const string rootKey = @"Software\Mozilla\Mozilla Firefox";

                var currentVersionObj = isHklm
                    ? _os.WindowsRegistryAdapter.RetrieveLocalMachineValue(rootKey, "CurrentVersion")
                    : _os.WindowsRegistryAdapter.RetrieveCurrentUserValue(rootKey, "CurrentVersion");

                if (currentVersionObj is null) return null;

                string currentVersion = currentVersionObj.ToString()!;
                string mainKeyPath = $@"{rootKey}\{currentVersion}\Main";

                var pathToExeObj = isHklm
                    ? _os.WindowsRegistryAdapter.RetrieveLocalMachineValue(mainKeyPath, "PathToExe")
                    : _os.WindowsRegistryAdapter.RetrieveCurrentUserValue(mainKeyPath, "PathToExe");

                return pathToExeObj?.ToString();
            }
        }
    }

    public override string ProcessName => "firefox";
    protected override string RegistryKeyVersion => @"Software\Mozilla\Mozilla Firefox";

    public override BrowserPaths ResolvePaths()
    {
        var appData = _os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("AppData");
        var localAppData = _os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("LocalAppData");

        var firefoxRoamingRoot = _os.WindowsFileSystemServiceAdapter.CombinePaths(appData, "Mozilla", "Firefox");
        var firefoxLocalRoot = _os.WindowsFileSystemServiceAdapter.CombinePaths(localAppData, "Mozilla", "Firefox");

        var cacheDirs = new List<string>();
        var cookiesDirs = new List<string>();
        var extensionsDirs = new List<string>();

        var profilesIniPath = _os.WindowsFileSystemServiceAdapter.CombinePaths(firefoxRoamingRoot, "profiles.ini");
        var profileInfos = RetrieveProfileFoldersFromIni(profilesIniPath);

        foreach (var profile in profileInfos)
        {
            string fullProfilePathRoaming;
            string fullProfilePathLocal;

            if (profile.IsRelative)
            {
                fullProfilePathRoaming = _os.WindowsFileSystemServiceAdapter.CombinePaths(firefoxRoamingRoot, profile.Path);
                fullProfilePathLocal = _os.WindowsFileSystemServiceAdapter.CombinePaths(firefoxLocalRoot, profile.Path);
            }
            else
            {
                fullProfilePathRoaming = profile.Path;

                var folderName = new DirectoryInfo(profile.Path).Name;
                fullProfilePathLocal = _os.WindowsFileSystemServiceAdapter.CombinePaths(firefoxLocalRoot, "Profiles", folderName);
            }
            cacheDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(fullProfilePathLocal, "cache2", "entries"));

            cacheDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(fullProfilePathLocal, "startupCache"));

            cookiesDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(fullProfilePathRoaming, "storage", "default"));

            extensionsDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(fullProfilePathRoaming, "extensions"));
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
            if (!_os.WindowsFileSystemServiceAdapter.DirectoryExists(extensionsDir)) continue;

            var xpiPath = _os.WindowsFileSystemServiceAdapter.CombinePaths(extensionsDir, idToCheck);
            if (_os.WindowsFileSystemServiceAdapter.FileExists(xpiPath)) return true;

            var folderName = idToCheck.Replace(".xpi", "", StringComparison.OrdinalIgnoreCase);
            var folderPath = _os.WindowsFileSystemServiceAdapter.CombinePaths(extensionsDir, folderName);
            if (_os.WindowsFileSystemServiceAdapter.DirectoryExists(folderPath)) return true;
        }

        return false;
    }

    private List<ProfileInfo> RetrieveProfileFoldersFromIni(string iniPath)
    {
        var profiles = new List<ProfileInfo>();

        if (!_os.WindowsFileSystemServiceAdapter.FileExists(iniPath)) return profiles;

        try
        {
            var lines = _os.WindowsFileSystemServiceAdapter.ReadAllLines(iniPath);

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
            _logger.LogError(ex, "Fehler beim Parsen der profiles.ini");
        }

        return profiles;
    }

    private sealed class ProfileInfo
    {
        public string Path { get; set; } = string.Empty;
        public bool IsRelative { get; set; } = true;
    }
}


