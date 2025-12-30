using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Infrastructure.Browsers
{
    public class FirefoxBrowser : BrowserBase
    {
        public override string DisplayName => "Firefox";
        public override string IconPath => "/Resources/Visuals/Icons/Intersection/fa_firefox.png";
        public override string DownloadUrl => WebLinks.FirefoxDownloadLink;
        public override BrowserType Type => BrowserType.Firefox;
        protected override string ProcessName => "firefox";

        protected override string RegistryKeyVersion => @"Software\Mozilla\Mozilla Firefox";

        public FirefoxBrowser(IOperatingSystemFacade os, ILogger<FirefoxBrowser> logger)
            : base(os, logger) { }

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

                return [.. paths.Distinct()];
            }
        }

        public override BrowserPaths GetPaths()
        {
            var appData = _os.WindowsFileSystemService.GetEnvironmentPath("AppData");
            var firefoxRoot = _os.WindowsFileSystemService.CombinePaths(appData, "Mozilla", "Firefox");

            var profileName = GetProfileNameFromIni(_os.WindowsFileSystemService.CombinePaths(firefoxRoot, "profiles.ini"));

            var profilePathRoaming = _os.WindowsFileSystemService.CombinePaths(firefoxRoot, "Profiles", profileName);
            var profilePathLocal = _os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData"), "Mozilla", "Firefox", "Profiles", profileName);

            return new BrowserPaths(
                CacheDir: _os.WindowsFileSystemService.CombinePaths(profilePathLocal, "cache2", "entries"),
                CookiesDir: _os.WindowsFileSystemService.CombinePaths(profilePathRoaming, "storage", "default"),
                ExtensionsDir: _os.WindowsFileSystemService.CombinePaths(profilePathRoaming, "extensions")
            );
        }

        private string GetProfileNameFromIni(string iniPath)
        {
            if (!_os.WindowsFileSystemService.FileExists(iniPath)) return "Default";


            return "Default";
        }
    }
}
