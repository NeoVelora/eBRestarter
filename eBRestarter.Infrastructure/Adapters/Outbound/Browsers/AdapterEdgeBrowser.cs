using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.Common.Statics;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.Browsers;

public sealed class AdapterEdgeBrowser(IOutboundPortOsProcessControl processControlPort, IOutboundPortSystemConfigurationRepository settingsPort, IOutboundPortFileSystem fileSystemPort, ILogger<AdapterEdgeBrowser> logger) : AdapterChromiumBrowserBase(processControlPort, settingsPort, fileSystemPort, logger)
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


