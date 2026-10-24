using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.Common.Attributes;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelBrowserAddonStatus : ObservableObject
{
    private const string AddonPlaceholderDash = "-";
    private const string AddonStatusColorExtensionInstalledHex = "#7ED422";
    private const string AddonStatusColorExtensionNotInstalledHex = "#E40E87";
    private const string AddonStatusIconDefault = "?";
    private const string AddonStatusIconEmpty = "";
    private const string AddonStatusIconExtensionInstalled = "\u2713";
    private const string AddonStatusIconExtensionNotInstalled = "\u2717";
    private const string InstallStatusColorBrowserInstalledHex = "#2e7d32";
    private const string InstallStatusColorBrowserNotInstalledHex = "#BA224D";
    private const string KeyAddonBrowserInstalled = "Addon_BrowserInstalled";
    private const string KeyAddonBrowserNotInstalled = "Addon_BrowserNotInstalled";
    private const string KeyAddonBtnBrowserMissing = "Addon_BtnBrowserMissing";
    private const string KeyAddonBtnInstall = "Addon_BtnInstall";
    private const string KeyAddonBtnManage = "Addon_BtnManage";
    private const string KeyAddonExtensionInstalled = "Addon_ExtensionInstalled";
    private const string KeyAddonExtensionNotInstalled = "Addon_ExtensionNotInstalled";
    private const string KeyAddonLoading = "Addon_Loading";
    private const string KeyAddonStatusChecking = "Addon_StatusChecking";
    private const string NeutralForegroundHex = "#000000";

    private readonly IOutboundPortBrowser _browser;
    private readonly ILogger<ViewModelBrowserAddonStatus> _logger;
    private readonly IInboundPortLocalizationProvider _localizationService;

    private readonly DispatcherQueue _dispatcherQueue;


    public ViewModelBrowserAddonStatus(
        IOutboundPortBrowser browser,
        IInboundPortLocalizationProvider localizationService,
        ILogger<ViewModelBrowserAddonStatus> logger)
    {
        ArgumentNullException.ThrowIfNull(browser);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(logger);

        _browser = browser;
        _localizationService = localizationService;
        _logger = logger;

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException($"{nameof(ViewModelBrowserAddonStatus)} must be constructed on a thread with a WinUI DispatcherQueue (UI thread).");

        BrowserName = _browser.DisplayName;
        IconPath = _browser.IconPath;
        AddonStatusColor = NeutralForegroundHex;
        AddonStatusIcon = AddonStatusIconDefault;
        InstallStatusColor = NeutralForegroundHex;
        AddonStatusText = _localizationService.RetrieveString(KeyAddonStatusChecking);
        InstallStatusText = _localizationService.RetrieveString(KeyAddonStatusChecking);
        ButtonText = _localizationService.RetrieveString(KeyAddonLoading);

        RefreshStatusAsync().Forget(_logger, nameof(RefreshStatusAsync));
    }


    [ObservableProperty]
    public partial string AddonStatusColor { get; set; }

    [ObservableProperty]
    public partial string AddonStatusIcon { get; set; }

    [ObservableProperty]
    public partial string AddonStatusText { get; set; }

    [ObservableProperty]
    public partial string BrowserName { get; set; }

    [ObservableProperty]
    public partial string ButtonText { get; set; }

    [ObservableProperty]
    public partial string IconPath { get; set; }

    [ObservableProperty]
    public partial string InstallStatusColor { get; set; }

    [ObservableProperty]
    public partial string InstallStatusText { get; set; }

    [ObservableProperty]
    public partial bool IsButtonEnabled { get; set; }


    [RelayCommand]
    private void OpenStore()
    {
        if (string.IsNullOrWhiteSpace(_browser.ExtensionInstallUrl))
        {
            return;
        }

        if (!_browser.Start(_browser.ExtensionInstallUrl))
        {
            _logger.LogWarning(
                LogEventIds.Browser.BrowserStartFailed,
                "Could not open the extension store for {BrowserType}; the browser did not start.",
                _browser.Type);
        }
    }

    [ExceptionBoundary(ExceptionBoundaryKind.ThreadRoot, Justification = "Background probe boundary for refreshing individual browser add-on installation status.")]
    public async Task RefreshStatusAsync()
    {
        BrowserAddonProbeResult probeResult;

        try
        {
            probeResult = await Task.Run(Probe).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(
                LogEventIds.Browser.BrowserProfileDiscoveryFailed,
                exception,
                "Probing add-on status for {BrowserType} failed.",
                _browser.Type);

            return;
        }

        _dispatcherQueue.TryEnqueue(() => ApplyProbeResult(probeResult));
    }

    private void ApplyProbeResult(BrowserAddonProbeResult probeResult)
    {
        if (!probeResult.IsInstalled)
        {
            string notInstalledFormat = _localizationService.RetrieveString(KeyAddonBrowserNotInstalled);

            InstallStatusText = string.Format(CultureInfo.CurrentCulture, notInstalledFormat, BrowserName);
            InstallStatusColor = InstallStatusColorBrowserNotInstalledHex;
            AddonStatusText = AddonPlaceholderDash;
            AddonStatusIcon = AddonStatusIconEmpty;
            IsButtonEnabled = false;
            ButtonText = _localizationService.RetrieveString(KeyAddonBtnBrowserMissing);

            return;
        }

        string installedFormat = _localizationService.RetrieveString(KeyAddonBrowserInstalled);

        InstallStatusText = string.Format(CultureInfo.CurrentCulture, installedFormat, BrowserName);
        InstallStatusColor = InstallStatusColorBrowserInstalledHex;
        IsButtonEnabled = true;

        if (probeResult.IsExtensionInstalled)
        {
            AddonStatusText = _localizationService.RetrieveString(KeyAddonExtensionInstalled);
            AddonStatusIcon = AddonStatusIconExtensionInstalled;
            AddonStatusColor = AddonStatusColorExtensionInstalledHex;
            ButtonText = _localizationService.RetrieveString(KeyAddonBtnManage);
        }
        else
        {
            AddonStatusText = _localizationService.RetrieveString(KeyAddonExtensionNotInstalled);
            AddonStatusIcon = AddonStatusIconExtensionNotInstalled;
            AddonStatusColor = AddonStatusColorExtensionNotInstalledHex;
            ButtonText = _localizationService.RetrieveString(KeyAddonBtnInstall);
        }
    }

    private BrowserAddonProbeResult Probe()
    {
        bool isInstalled = _browser.IsInstalled;

        bool isExtensionInstalled = isInstalled && _browser.IsExtensionInstalled(string.Empty);

        return new BrowserAddonProbeResult(isInstalled, isExtensionInstalled);
    }


    private readonly record struct BrowserAddonProbeResult(bool IsInstalled, bool IsExtensionInstalled);
}
