using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;

public sealed class AdapterChromeBrowserWrapper(
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortSystemConfigurationRepository settingsPort,
    IOutboundPortFileSystem fileSystemPort,
    ILogger<AdapterChromeBrowserWrapper> logger)
    : AdapterChromiumBrowserBaseWrapper(
        processControlPort,
        settingsPort,
        fileSystemPort,
        logger)
{

    private const string DefaultBrowserRegistryName = "Google Chrome";
    private const string DefaultDisplayName = "Chrome";
    private const string DefaultExeFileName = "chrome.exe";
    private const string DefaultExtensionId = "agchmcconfdfcenopioeilpgjngelefk";
    private const string DefaultIconPath = "ms-appx:///Resources/Visuals/Icons/Intersection/fa_chrome.png";
    private const string DefaultProcessName = "chrome";
    private const string DefaultProgramFilesSubPath = @"Google\Chrome\Application";
    private const string DefaultRegistryKeyVersion = @"Software\Google\Chrome\BLBeacon";
    private const string DefaultUninstallSubKey = "Google Chrome";

    private static readonly string[] _userDataSubPath = ["Google", "Chrome", "User Data"];


    protected override string BrowserRegistryName => DefaultBrowserRegistryName;
    public override string DisplayName => DefaultDisplayName;
    public override string DownloadUrl => WebLinks.ChromeDownloadLinkDE;
    protected override string ExeFileName => DefaultExeFileName;
    protected override string ExtensionId => DefaultExtensionId;
    public override string ExtensionInstallUrl => WebLinks.ChromeEVisitorAddOnLink;
    public override string IconPath => DefaultIconPath;
    public override string ProcessName => DefaultProcessName;
    protected override string ProgramFilesSubPath => DefaultProgramFilesSubPath;
    protected override string RegistryKeyVersion => DefaultRegistryKeyVersion;
    protected override string UninstallSubKey => DefaultUninstallSubKey;

    public override BrowserType Type => BrowserType.Chrome;

    protected override string[] UserDataSubPath => _userDataSubPath;
}
