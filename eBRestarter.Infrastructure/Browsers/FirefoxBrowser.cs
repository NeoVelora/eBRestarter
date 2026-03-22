using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers;

public class FirefoxBrowser(IOperatingSystemFacade os, ILogger<FirefoxBrowser> logger) : BrowserBase(os, logger)
{
    private const string EbesucherAddOnNameForFirefox = "{fef425dc-a60f-4484-954d-71ecf2544846}.xpi";

    public override string DisplayName => "Firefox";
    public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_firefox.png";
    public override string DownloadUrl => WebLinks.FirefoxDownloadLink;
    public override string ExtensionInstallUrl => WebLinks.FirefoxEVisitorAddOnLink;
    public override BrowserType Type => BrowserType.Firefox;
    protected override string ProcessName => "firefox";
    protected override string RegistryKeyVersion => @"Software\Mozilla\Mozilla Firefox";

    public override bool IsExtensionInstalled(string? extensionId = null)
    {
        var paths = GetPaths();

        if (paths.ExtensionsDirs == null || paths.ExtensionsDirs.Count == 0) return false;

        var idToCheck = string.IsNullOrEmpty(extensionId)
            ? EbesucherAddOnNameForFirefox.TrimStart('\\')
            : extensionId;

        foreach (var extensionsDir in paths.ExtensionsDirs)
        {
            if (!_os.WindowsFileSystemService.DirectoryExists(extensionsDir)) continue;

            var xpiPath = _os.WindowsFileSystemService.CombinePaths(extensionsDir, idToCheck);
            if (_os.WindowsFileSystemService.FileExists(xpiPath)) return true;

            var folderName = idToCheck.Replace(".xpi", "", StringComparison.OrdinalIgnoreCase);
            var folderPath = _os.WindowsFileSystemService.CombinePaths(extensionsDir, folderName);
            if (_os.WindowsFileSystemService.DirectoryExists(folderPath)) return true;
        }

        return false;
    }

    public override BrowserPaths GetPaths()
    {
        var appData = _os.WindowsFileSystemService.GetEnvironmentPath("AppData");
        var localAppData = _os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData");

        var firefoxRoamingRoot = _os.WindowsFileSystemService.CombinePaths(appData, "Mozilla", "Firefox");
        var firefoxLocalRoot = _os.WindowsFileSystemService.CombinePaths(localAppData, "Mozilla", "Firefox");

        var cacheDirs = new List<string>();
        var cookiesDirs = new List<string>();
        var extensionsDirs = new List<string>();

        var profilesIniPath = _os.WindowsFileSystemService.CombinePaths(firefoxRoamingRoot, "profiles.ini");
        var profileInfos = GetProfileFoldersFromIni(profilesIniPath);

        foreach (var profile in profileInfos)
        {
            string fullProfilePathRoaming;
            string fullProfilePathLocal;

            if (profile.IsRelative)
            {
                fullProfilePathRoaming = _os.WindowsFileSystemService.CombinePaths(firefoxRoamingRoot, profile.Path);
                fullProfilePathLocal = _os.WindowsFileSystemService.CombinePaths(firefoxLocalRoot, profile.Path);
            }
            else
            {
                fullProfilePathRoaming = profile.Path;

                var folderName = new DirectoryInfo(profile.Path).Name;
                fullProfilePathLocal = _os.WindowsFileSystemService.CombinePaths(firefoxLocalRoot, "Profiles", folderName);
            }

            cacheDirs.Add(_os.WindowsFileSystemService.CombinePaths(fullProfilePathLocal, "cache2", "entries"));
            cacheDirs.Add(_os.WindowsFileSystemService.CombinePaths(fullProfilePathLocal, "startupCache"));

            cookiesDirs.Add(_os.WindowsFileSystemService.CombinePaths(fullProfilePathRoaming, "storage", "default"));

            extensionsDirs.Add(_os.WindowsFileSystemService.CombinePaths(fullProfilePathRoaming, "extensions"));
        }

        return new BrowserPaths(cacheDirs, cookiesDirs, extensionsDirs);
    }


    private class ProfileInfo
    {
        public string Path { get; set; } = string.Empty;
        public bool IsRelative { get; set; } = true;
    }

    private List<ProfileInfo> GetProfileFoldersFromIni(string iniPath)
    {
        var profiles = new List<ProfileInfo>();

        if (!_os.WindowsFileSystemService.FileExists(iniPath)) return profiles;

        try
        {
            var lines = File.ReadAllLines(iniPath);

            string? currentPath = null;
            bool? currentIsRelative = null;

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("[") && line.Trim().EndsWith("]"))
                {
                    if (currentPath != null)
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
                    currentPath = line.Substring(5).Trim();
                }
                else if (line.StartsWith("IsRelative=", StringComparison.OrdinalIgnoreCase))
                {
                    currentIsRelative = line.Trim().EndsWith("1");
                }
            }

            if (currentPath != null)
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

    protected override List<string> ExecutablePaths
    {
        get
        {
            var paths = new List<string>();

            string? GetPathFromMozillaRegistry(bool isHklm)
            {
                string rootKey = @"Software\Mozilla\Mozilla Firefox";

                var currentVersionObj = isHklm
                    ? _os.WindowsRegistryService.GetLocalMachineValue(rootKey, "CurrentVersion")
                    : _os.WindowsRegistryService.GetCurrentUserValue(rootKey, "CurrentVersion");

                if (currentVersionObj == null) return null;

                string currentVersion = currentVersionObj.ToString()!;
                string mainKeyPath = $@"{rootKey}\{currentVersion}\Main";

                var pathToExeObj = isHklm
                    ? _os.WindowsRegistryService.GetLocalMachineValue(mainKeyPath, "PathToExe")
                    : _os.WindowsRegistryService.GetCurrentUserValue(mainKeyPath, "PathToExe");

                return pathToExeObj?.ToString();
            }

            var hkcuPath = GetPathFromMozillaRegistry(false);
            if (!string.IsNullOrEmpty(hkcuPath)) paths.Add(hkcuPath);

            var hklmPath = GetPathFromMozillaRegistry(true);
            if (!string.IsNullOrEmpty(hklmPath)) paths.Add(hklmPath);

            paths.Add(_os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("ProgramFiles"), @"Mozilla Firefox\firefox.exe"));
            paths.Add(_os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("ProgramFiles(x86)"), @"Mozilla Firefox\firefox.exe"));
            paths.Add(_os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData"), @"Mozilla Firefox\firefox.exe"));

            return paths.Distinct().ToList();
        }
    }
}
