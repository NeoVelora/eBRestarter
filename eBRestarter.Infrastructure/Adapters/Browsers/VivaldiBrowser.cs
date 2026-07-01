using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Browser;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Browsers;

public sealed class VivaldiBrowser(IOsProcessControlOutboundPort processControlPort, ISettingsRepositoryOutboundPort settingsPort, IFileSystemOutboundPort fileSystemPort, ILogger<VivaldiBrowser> logger) : ChromiumBrowserBase(processControlPort, settingsPort, fileSystemPort, logger)
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

            var version = _settingsPort.GetUserValue(uninstallPathCU, "DisplayVersion");

            version ??= _settingsPort.GetSystemValue(uninstallPathLM, "DisplayVersion")
                ?? _settingsPort.GetSystemValue(uninstallPathWow, "DisplayVersion");

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
    protected override string[] UserDataSubPath => ["Vivaldi", "User Data"];
}


