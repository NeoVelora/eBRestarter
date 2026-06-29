using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Browser;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Browsers;

public sealed class BraveBrowser(IOsProcessControlPort processControlPort, ISettingsPort settingsPort, IFileSystemPort fileSystemPort, ILogger<BraveBrowser> logger) : ChromiumBrowserBase(processControlPort, settingsPort, fileSystemPort, logger)
{
    public override string DisplayName => "Brave";
    public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_brave.png";
    public override string DownloadUrl => WebLinks.BraveDownloadLinkDE;

    public override BrowserType Type => BrowserType.Brave;

    public override string ExtensionInstallUrl => "https://chrome.google.com/webstore/detail/ebesucher-addon/agchmcconfdfcenopioeilpgjngelefk";

    protected override string ExeFileName => "brave.exe";
    protected override string BrowserRegistryName => "Brave";
    protected override string UninstallSubKey => "BraveSoftware Brave-Browser";
    protected override string ProgramFilesSubPath => @"BraveSoftware\Brave-Browser\Application";

    public override string ProcessName => "brave";

    protected override string RegistryKeyVersion => @"Software\BraveSoftware\Brave-Browser\BLBeacon";
    protected override string ExtensionId => "agchmcconfdfcenopioeilpgjngelefk";
    protected override string[] UserDataSubPath => ["BraveSoftware", "Brave-Browser", "User Data"];
}


