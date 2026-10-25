using System.Globalization;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;

public sealed class AdapterVivaldiBrowserWrapper(
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortSystemConfigurationRepository settingsPort,
    IOutboundPortFileSystem fileSystemPort,
    ILogger<AdapterVivaldiBrowserWrapper> logger)
    : AdapterChromiumBrowserBaseWrapper(
        processControlPort,
        settingsPort,
        fileSystemPort,
        logger)
{

    private const string DefaultBrowserRegistryName = "Vivaldi";
    private const string DefaultDisplayName = "Vivaldi";
    private const string DefaultExeFileName = "vivaldi.exe";
    private const string DefaultExtensionId = "agchmcconfdfcenopioeilpgjngelefk";
    private const string DefaultIconPath = "ms-appx:///Resources/Visuals/Icons/Intersection/icons8_vivaldi.png";
    private const string DefaultProcessName = "vivaldi";
    private const string DefaultProgramFilesSubPath = @"Vivaldi\Application";
    private const string DefaultRegistryKeyVersion = @"Software\Vivaldi\BLBeacon";
    private const string DefaultUninstallSubKey = "Vivaldi";
    private const string DisplayVersionValueName = "DisplayVersion";
    private const string MachineUninstallRegistryKeyPattern = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}";
    private const string Wow64UninstallRegistryKeyPattern = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{0}";

    private static readonly string[] _userDataSubPath = ["Vivaldi", "User Data"];


    protected override string BrowserRegistryName => DefaultBrowserRegistryName;

    public override string BrowserVersion
    {
        get
        {
            var uninstallPath = string.Format(CultureInfo.InvariantCulture, MachineUninstallRegistryKeyPattern, UninstallSubKey);
            var uninstallPathWow = string.Format(CultureInfo.InvariantCulture, Wow64UninstallRegistryKeyPattern, UninstallSubKey);

            var versionObj = _settingsPort.GetUserValue(uninstallPath, DisplayVersionValueName)
                ?? _settingsPort.GetSystemValue(uninstallPath, DisplayVersionValueName)
                ?? _settingsPort.GetSystemValue(uninstallPathWow, DisplayVersionValueName);

            if (versionObj?.ToString() is { Length: > 0 } versionString)
            {
                return CleanVersionString(versionString);
            }

            return base.BrowserVersion;
        }
    }

    public override string DisplayName => DefaultDisplayName;
    public override string DownloadUrl => WebLinks.VivaldiDownloadLinkDE;
    protected override string ExeFileName => DefaultExeFileName;
    protected override string ExtensionId => DefaultExtensionId;
    public override string ExtensionInstallUrl => WebLinks.ChromeEVisitorAddOnLink;
    public override string IconPath => DefaultIconPath;
    public override string ProcessName => DefaultProcessName;
    protected override string ProgramFilesSubPath => DefaultProgramFilesSubPath;
    protected override string RegistryKeyVersion => DefaultRegistryKeyVersion;
    protected override string UninstallSubKey => DefaultUninstallSubKey;

    public override BrowserType Type => BrowserType.Vivaldi;

    protected override string[] UserDataSubPath => _userDataSubPath;
}
