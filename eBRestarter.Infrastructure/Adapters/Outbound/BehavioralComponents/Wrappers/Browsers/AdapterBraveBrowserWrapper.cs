using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;

public sealed class AdapterBraveBrowserWrapper(
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortSystemConfigurationRepository settingsPort,
    IOutboundPortFileSystem fileSystemPort,
    ILogger<AdapterBraveBrowserWrapper> logger)
    : AdapterChromiumBrowserBaseWrapper(
        processControlPort,
        settingsPort,
        fileSystemPort,
        logger)
{

    private const string ChromeWebStoreExtensionBaseUrl = "https://chrome.google.com/webstore/detail/ebesucher-addon/";
    private const string DefaultBrowserRegistryName = "Brave";
    private const string DefaultDisplayName = "Brave";
    private const string DefaultExeFileName = "brave.exe";
    private const string DefaultExtensionId = "agchmcconfdfcenopioeilpgjngelefk";
    private const string DefaultIconPath = "ms-appx:///Resources/Visuals/Icons/Intersection/fa_brave.png";
    private const string DefaultProcessName = "brave";
    private const string DefaultProgramFilesSubPath = @"BraveSoftware\Brave-Browser\Application";
    private const string DefaultRegistryKeyVersion = @"Software\BraveSoftware\Brave-Browser\BLBeacon";
    private const string DefaultUninstallSubKey = "BraveSoftware Brave-Browser";

    private static readonly string[] _userDataSubPath = ["BraveSoftware", "Brave-Browser", "User Data"];


    protected override string BrowserRegistryName => DefaultBrowserRegistryName;
    public override string DisplayName => DefaultDisplayName;
    public override string DownloadUrl => WebLinks.BraveDownloadLinkDE;
    protected override string ExeFileName => DefaultExeFileName;
    protected override string ExtensionId => DefaultExtensionId;
    public override string ExtensionInstallUrl => $"{ChromeWebStoreExtensionBaseUrl}{ExtensionId}";
    public override string IconPath => DefaultIconPath;
    public override string ProcessName => DefaultProcessName;
    protected override string ProgramFilesSubPath => DefaultProgramFilesSubPath;
    protected override string RegistryKeyVersion => DefaultRegistryKeyVersion;
    protected override string UninstallSubKey => DefaultUninstallSubKey;

    public override BrowserType Type => BrowserType.Brave;

    protected override string[] UserDataSubPath => _userDataSubPath;
}
