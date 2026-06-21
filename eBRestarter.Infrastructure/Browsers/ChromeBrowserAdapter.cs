using eBRestarter.Core.Application.Constants;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers;

public class ChromeBrowserAdapter(IOperatingSystemFacade os, ILogger<ChromeBrowserAdapter> logger) : ChromiumBrowserBaseAdapter(os, logger)
{
    public override BrowserType Type => BrowserType.Chrome;
    public override string DisplayName => "Chrome";
    public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_chrome.png";
    public override string DownloadUrl => WebLinks.ChromeDownloadLinkDE;
    public override string ExtensionInstallUrl => WebLinks.ChromeEVisitorAddOnLink;

    protected override string ExeFileName => "chrome.exe";
    protected override string BrowserRegistryName => "Google Chrome";
    protected override string UninstallSubKey => "Google Chrome";
    protected override string ProgramFilesSubPath => @"Google\Chrome\Application";


    public override string ProcessName => "chrome";
    protected override string RegistryKeyVersion => @"Software\Google\Chrome\BLBeacon";
    protected override string ExtensionId => "agchmcconfdfcenopioeilpgjngelefk";


    public override BrowserPaths ResolvePaths()
    {
        var localAppData = _os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("LocalAppData");

        var userDataRoot = _os.WindowsFileSystemServiceAdapter.CombinePaths(localAppData, "Google", "Chrome", "User Data");

        var cacheDirs = new List<string>();
        var cookiesDirs = new List<string>();
        var extensionsDirs = new List<string>();

        var allProfileFolders = new List<string>();

        var defaultPath = _os.WindowsFileSystemServiceAdapter.CombinePaths(userDataRoot, "Default");
        if (_os.WindowsFileSystemServiceAdapter.DirectoryExists(defaultPath))
            allProfileFolders.Add(defaultPath);

        try
        {
            var dirs = System.IO.Directory.GetDirectories(userDataRoot, "Profile *");
            allProfileFolders.AddRange(dirs);
        }
        catch { }

        foreach (var profilePath in allProfileFolders)
        {
            cacheDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "Cache", "Cache_Data"));
            cacheDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "Service Worker"));

            cacheDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "Code Cache"));
            cacheDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "GPUCache"));

            cookiesDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "IndexedDB"));
            cookiesDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "Network"));

            cookiesDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "Local Storage"));
            cookiesDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "Session Storage"));

            extensionsDirs.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(profilePath, "Extensions", ExtensionId));
        }

        return new BrowserPaths(cacheDirs, cookiesDirs, extensionsDirs);
    }


}


