using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Browser;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Browsers;

public sealed class EdgeBrowser(IOsProcessControlPort processControlPort, ISettingsPort settingsPort, IFileSystemPort fileSystemPort, ILogger<EdgeBrowser> logger) : ChromiumBrowserBase(processControlPort, settingsPort, fileSystemPort, logger)
{
    public override string DisplayName => "Edge";
    public override string IconPath => "ms-appx:///Resources/Visuals/Icons/Intersection/fa_edge.png";
    public override string DownloadUrl => WebLinks.EdgeDownloadLinkDE;
    public override string ExtensionInstallUrl => WebLinks.EdgeEVisitorAddOnLink;

    public override BrowserType Type => BrowserType.Edge;

    public override string ProcessName => "msedge";

    protected override string RegistryKeyVersion => @"Software\Microsoft\Edge\BLBeacon";
    protected override string ExtensionId => "kjhejmaladginnedpoppohfnkionnghi";
    protected override string[] UserDataSubPath => ["Microsoft", "Edge", "User Data"];

    protected override string ExeFileName => "msedge.exe";

    protected override string BrowserRegistryName => "Microsoft Edge";

    protected override string UninstallSubKey => "Microsoft Edge";

    protected override string ProgramFilesSubPath => @"Microsoft\Edge\Application";
}


