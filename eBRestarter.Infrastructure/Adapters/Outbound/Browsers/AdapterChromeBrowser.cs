using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.Common.Statics;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.Browsers;

public sealed class AdapterChromeBrowser(IOutboundPortOsProcessControl processControlPort, IOutboundPortSystemConfigurationRepository settingsPort, IOutboundPortFileSystem fileSystemPort, ILogger<AdapterChromeBrowser> logger) : AdapterChromiumBrowserBase(processControlPort, settingsPort, fileSystemPort, logger)
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
    protected override string[] UserDataSubPath => ["Google", "Chrome", "User Data"];
}


