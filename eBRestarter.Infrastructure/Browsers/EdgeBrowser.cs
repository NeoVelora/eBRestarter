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
    public class EdgeBrowser : ChromiumBrowserBase
    {
        public override string DisplayName => "Edge";
        public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_edge.png";
        public override string DownloadUrl => WebLinks.EdgeDownloadLinkDE;

        public override BrowserType Type => BrowserType.Edge;

        protected override string ProcessName => "msedge";

        protected override string RegistryKeyVersion => @"Software\Microsoft\Edge\BLBeacon";

        protected override string ExtensionId => "agchmcconfdfcenopioeilpgjngelefk";
        public override string ExtensionInstallUrl => "https://chrome.google.com/webstore/detail/ebesucher-addon/agchmcconfdfcenopioeilpgjngelefk";

        public EdgeBrowser(IOperatingSystemFacade os, ILogger<EdgeBrowser> logger) : base(os, logger) { }

        protected override string ExeFileName => "msedge.exe";

        protected override string BrowserRegistryName => "Microsoft Edge";

        protected override string UninstallSubKey => "Microsoft Edge";

        protected override string ProgramFilesSubPath => @"Microsoft\Edge\Application";

        public override BrowserPaths GetPaths()
        {
            var localAppData = _os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData");
            var baseDir = _os.WindowsFileSystemService.CombinePaths(localAppData, "Microsoft", "Edge", "User Data", "Default");

            return new BrowserPaths(
                CacheDir: _os.WindowsFileSystemService.CombinePaths(baseDir, "Cache"),
                CookiesDir: _os.WindowsFileSystemService.CombinePaths(baseDir, "IndexedDB"),
                ExtensionsDir: _os.WindowsFileSystemService.CombinePaths(baseDir, "Extensions", ExtensionId)
            );
        }
    }
}
