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
    public class BraveBrowser : ChromiumBrowserBase
    {

        protected override string ExeFileName => "brave.exe";
        protected override string BrowserRegistryName => "Brave";
        protected override string UninstallSubKey => "BraveSoftware Brave-Browser";
        protected override string ProgramFilesSubPath => @"BraveSoftware\Brave-Browser\Application";

        public override string DisplayName => "Brave";
        public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_brave.png";
        public override string DownloadUrl => WebLinks.BraveDownloadLinkDE;

        public override BrowserType Type => BrowserType.Brave;

        protected override string ProcessName => "brave";

        protected override string RegistryKeyVersion => @"Software\BraveSoftware\Brave-Browser\BLBeacon";

        protected override string ExtensionId => "agchmcconfdfcenopioeilpgjngelefk";
        public override string ExtensionInstallUrl => "https://chrome.google.com/webstore/detail/ebesucher-addon/agchmcconfdfcenopioeilpgjngelefk";

        public BraveBrowser(IOperatingSystemFacade os, ILogger<BraveBrowser> logger) : base(os, logger) { }


        public override BrowserPaths GetPaths()
        {
            var localAppData = _os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData");

            var userDataRoot = _os.WindowsFileSystemService.CombinePaths(localAppData, "BraveSoftware", "Brave-Browser", "User Data");

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
