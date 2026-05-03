using eBRestarter.Core.Application.Constants;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers;

public class EdgeBrowser(IOperatingSystemFacade os, ILogger<EdgeBrowser> logger) : ChromiumBrowserBase(os, logger)
{
    public override string DisplayName => "Edge";
    public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_edge.png";
    public override string DownloadUrl => WebLinks.EdgeDownloadLinkDE;

    public override BrowserType Type => BrowserType.Edge;

    protected override string ProcessName => "msedge";

    protected override string RegistryKeyVersion => @"Software\Microsoft\Edge\BLBeacon";

    protected override string ExtensionId => "kjhejmaladginnedpoppohfnkionnghi";

    public override string ExtensionInstallUrl => WebLinks.EdgeEVisitorAddOnLink;

    protected override string ExeFileName => "msedge.exe";

    protected override string BrowserRegistryName => "Microsoft Edge";

    protected override string UninstallSubKey => "Microsoft Edge";

    protected override string ProgramFilesSubPath => @"Microsoft\Edge\Application";

    public override BrowserPaths GetPaths()
    {
        var localAppData = _os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData");

        var userDataRoot = _os.WindowsFileSystemService.CombinePaths(localAppData, "Microsoft", "Edge", "User Data");

        var cacheDirs = new List<string>();
        var cookiesDirs = new List<string>();
        var extensionsDirs = new List<string>();

        var allProfileFolders = new List<string>();

        var defaultPath = _os.WindowsFileSystemService.CombinePaths(userDataRoot, "Default");
        if (_os.WindowsFileSystemService.DirectoryExists(defaultPath))
        {
            allProfileFolders.Add(defaultPath);
        }

        try
        {
            var dirs = Directory.GetDirectories(userDataRoot, "Profile *");
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
