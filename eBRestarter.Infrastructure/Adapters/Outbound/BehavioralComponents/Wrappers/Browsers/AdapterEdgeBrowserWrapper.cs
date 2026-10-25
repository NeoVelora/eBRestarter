using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;

public sealed class AdapterEdgeBrowserWrapper(
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortSystemConfigurationRepository settingsPort,
    IOutboundPortFileSystem fileSystemPort,
    ILogger<AdapterEdgeBrowserWrapper> logger)
    : AdapterChromiumBrowserBaseWrapper(
        processControlPort,
        settingsPort,
        fileSystemPort,
        logger)
{

    private const string DefaultBrowserRegistryName = "Microsoft Edge";
    private const string DefaultDisplayName = "Edge";
    private const string DefaultExeFileName = "msedge.exe";
    private const string DefaultExtensionId = "kjhejmaladginnedpoppohfnkionnghi";
    private const string DefaultIconPath = "ms-appx:///Resources/Visuals/Icons/Intersection/fa_edge.png";
    private const string DefaultProcessName = "msedge";
    private const string DefaultProgramFilesSubPath = @"Microsoft\Edge\Application";
    private const string DefaultRegistryKeyVersion = @"Software\Microsoft\Edge\BLBeacon";
    private const string DefaultUninstallSubKey = "Microsoft Edge";

    private static readonly string[] _userDataSubPath = ["Microsoft", "Edge", "User Data"];


    protected override string BrowserRegistryName => DefaultBrowserRegistryName;
    public override string DisplayName => DefaultDisplayName;
    public override string DownloadUrl => WebLinks.EdgeDownloadLinkDE;
    protected override string ExeFileName => DefaultExeFileName;
    protected override string ExtensionId => DefaultExtensionId;
    public override string ExtensionInstallUrl => WebLinks.EdgeEVisitorAddOnLink;
    public override string IconPath => DefaultIconPath;
    public override string ProcessName => DefaultProcessName;
    protected override string ProgramFilesSubPath => DefaultProgramFilesSubPath;
    protected override string RegistryKeyVersion => DefaultRegistryKeyVersion;
    protected override string UninstallSubKey => DefaultUninstallSubKey;

    public override BrowserType Type => BrowserType.Edge;

    protected override string[] UserDataSubPath => _userDataSubPath;
}
