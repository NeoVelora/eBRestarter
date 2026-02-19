using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace eBRestarter.Infrastructure.Browsers
{
    public class VivaldiBrowser : ChromiumBrowserBase
    {
        protected override string ExeFileName => "vivaldi.exe";
        protected override string BrowserRegistryName => "Vivaldi";
        protected override string UninstallSubKey => "Vivaldi";
        protected override string ProgramFilesSubPath => @"Vivaldi\Application";

        public override BrowserType Type => BrowserType.Vivaldi;
        public override string DisplayName => "Vivaldi";
        public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/icons8_vivaldi.png";

        public override string DownloadUrl => WebLinks.VivaldiDownloadLinkDE;

        protected override string ProcessName => "vivaldi";

        protected override string RegistryKeyVersion => @"Software\Vivaldi\BLBeacon";

        protected override string ExtensionId => "agchmcconfdfcenopioeilpgjngelefk";

        public override string ExtensionInstallUrl => WebLinks.ChromeEVisitorAddOnLink;

        public override string BrowserVersion
        {
            get
            {
                string uninstallPathCU = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}";
                string uninstallPathLM = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}";
                string uninstallPathWow = $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}";

                var version = _os.WindowsRegistryService.GetCurrentUserValue(uninstallPathCU, "DisplayVersion");

                version ??= _os.WindowsRegistryService.GetLocalMachineValue(uninstallPathLM, "DisplayVersion")
                           ?? _os.WindowsRegistryService.GetLocalMachineValue(uninstallPathWow, "DisplayVersion");

                if (version != null && !string.IsNullOrEmpty(version.ToString()))
                {
                    return CleanVersionString(version.ToString());
                }

                return base.BrowserVersion;
            }
        }

        public VivaldiBrowser(IOperatingSystemFacade os, ILogger<VivaldiBrowser> logger) : base(os, logger) { }

        public override BrowserPaths GetPaths()
        {
            var localAppData = _os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData");

            var userDataRoot = _os.WindowsFileSystemService.CombinePaths(localAppData, "Vivaldi", "User Data");

            var cacheDirs = new List<string>();
            var cookiesDirs = new List<string>();
            var extensionsDirs = new List<string>();

            var allProfileFolders = new List<string>();

            var defaultPath = _os.WindowsFileSystemService.CombinePaths(userDataRoot, "Default");
            if (_os.WindowsFileSystemService.DirectoryExists(defaultPath))
                allProfileFolders.Add(defaultPath);

            try
            {
                var dirs = System.IO.Directory.GetDirectories(userDataRoot, "Profile *");
                allProfileFolders.AddRange(dirs);
            }
            catch { }

            foreach (var profilePath in allProfileFolders)
            {
                cacheDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "Cache", "Cache_Data"));
                cacheDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "Service Worker"));
                cacheDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "Code Cache"));
                cacheDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "GPUCache"));

                cookiesDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "IndexedDB"));
                cookiesDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "Network"));
                cookiesDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "Local Storage"));
                cookiesDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "Session Storage"));

                extensionsDirs.Add(_os.WindowsFileSystemService.CombinePaths(profilePath, "Extensions", ExtensionId));
            }

            return new BrowserPaths(cacheDirs, cookiesDirs, extensionsDirs);
        }
    }
}