using eBRestarter.Core.Application.Constants;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers;

public class VivaldiBrowserAdapter(IOperatingSystemFacade os, ILogger<VivaldiBrowserAdapter> logger) : ChromiumBrowserBaseAdapter(os, logger)
{
    public override BrowserType Type => BrowserType.Vivaldi;
    public override string DisplayName => "Vivaldi";
    public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/icons8_vivaldi.png";

    public override string DownloadUrl => WebLinks.VivaldiDownloadLinkDE;

    public override string ExtensionInstallUrl => WebLinks.ChromeEVisitorAddOnLink;

    public override string BrowserVersion
    {
        get
        {
            string uninstallPathCU = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}";
            string uninstallPathLM = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}";
            string uninstallPathWow = $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}";

            var version = _os.WindowsRegistryAdapter.RetrieveCurrentUserValue(uninstallPathCU, "DisplayVersion");

            version ??= _os.WindowsRegistryAdapter.RetrieveLocalMachineValue(uninstallPathLM, "DisplayVersion")
                ?? _os.WindowsRegistryAdapter.RetrieveLocalMachineValue(uninstallPathWow, "DisplayVersion");

            if (version is not null && !string.IsNullOrEmpty(version.ToString()))
            {
                return CleanVersionString(version.ToString());
            }

            return base.BrowserVersion;
        }
    }

    protected override string ExeFileName => "vivaldi.exe";
    protected override string BrowserRegistryName => "Vivaldi";
    protected override string UninstallSubKey => "Vivaldi";
    protected override string ProgramFilesSubPath => @"Vivaldi\Application";

    public override string ProcessName => "vivaldi";

    protected override string RegistryKeyVersion => @"Software\Vivaldi\BLBeacon";
    protected override string ExtensionId => "agchmcconfdfcenopioeilpgjngelefk";


    public override BrowserPaths ResolvePaths()
    {
        var localAppData = _os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("LocalAppData");

        var userDataRoot = _os.WindowsFileSystemServiceAdapter.CombinePaths(localAppData, "Vivaldi", "User Data");

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


