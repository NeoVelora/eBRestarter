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
    public class EdgeBrowser : BrowserBase
    {
        public override BrowserType Type => BrowserType.Edge;

        protected override string ProcessName => "msedge";

        protected override string RegistryKeyVersion => @"Software\Microsoft\Edge\BLBeacon";

        private const string ExtensionId = "kjhejmaladginnedpoppohfnkionnghi";

        public EdgeBrowser(IOperatingSystemFacade os, ILogger<EdgeBrowser> logger)
            : base(os, logger) { }

        protected override List<string> ExecutablePaths => new()
        {
            _os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("ProgramFiles(x86)"), @"Microsoft\Edge\Application\msedge.exe"),
            _os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("ProgramFiles"), @"Microsoft\Edge\Application\msedge.exe")
        };

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
