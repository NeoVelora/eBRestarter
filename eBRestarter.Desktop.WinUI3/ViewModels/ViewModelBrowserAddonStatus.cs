using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using Microsoft.UI.Dispatching;
using System;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
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

        private const string NeutralForegroundHex = "#000000";

        private readonly IBrowserOutboundPort _browser;

        private readonly DispatcherQueue _dispatcherQueue;

        private readonly IInboundPortLocalizationProvider _localizationService;

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

        public ViewModelBrowserAddonStatus(
            IBrowserOutboundPort browser,
            IInboundPortLocalizationProvider LocalizationProvider)
        {
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(LocalizationProvider);

            _browser = browser;
            _localizationService = LocalizationProvider;

            _dispatcherQueue =
                DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException(
                    $"{nameof(ViewModelBrowserAddonStatus)} must be constructed on a thread with a WinUI DispatcherQueue (UI thread).");

            BrowserName = _browser.DisplayName;
            IconPath = _browser.IconPath;
            AddonStatusColor = NeutralForegroundHex;
            AddonStatusIcon = AddonStatusIconDefault;
            InstallStatusColor = NeutralForegroundHex;
            AddonStatusText = _localizationService.RetrieveString("Addon_StatusChecking");
            InstallStatusText = _localizationService.RetrieveString("Addon_StatusChecking");
            ButtonText = _localizationService.RetrieveString("Addon_Loading");

            RefreshStatus();
        }

        [RelayCommand]
        private void OpenStore()
        {
            if (!string.IsNullOrWhiteSpace(_browser.ExtensionInstallUrl))
                _browser.Start(_browser.ExtensionInstallUrl);
        }

        public void RefreshStatus()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                string installedFormat = _localizationService.RetrieveString("Addon_BrowserInstalled");
                string notInstalledFormat = _localizationService.RetrieveString("Addon_BrowserNotInstalled");

                if (!_browser.IsInstalled)
                {
                    InstallStatusText = string.Format(notInstalledFormat, BrowserName);
                    InstallStatusColor = InstallStatusColorBrowserNotInstalledHex;
                    AddonStatusText = AddonPlaceholderDash;
                    AddonStatusIcon = AddonStatusIconEmpty;
                    IsButtonEnabled = false;
                    ButtonText = _localizationService.RetrieveString("Addon_BtnBrowserMissing");
                    return;
                }

                InstallStatusText = string.Format(installedFormat, BrowserName);
                InstallStatusColor = InstallStatusColorBrowserInstalledHex;
                IsButtonEnabled = true;

                bool extensionIsInstalled = _browser.IsExtensionInstalled(string.Empty);

                if (extensionIsInstalled)
                {
                    AddonStatusText = _localizationService.RetrieveString("Addon_ExtensionInstalled");
                    AddonStatusIcon = AddonStatusIconExtensionInstalled;
                    AddonStatusColor = AddonStatusColorExtensionInstalledHex;
                    ButtonText = _localizationService.RetrieveString("Addon_BtnManage");
                }
                else
                {
                    AddonStatusText = _localizationService.RetrieveString("Addon_ExtensionNotInstalled");
                    AddonStatusIcon = AddonStatusIconExtensionNotInstalled;
                    AddonStatusColor = AddonStatusColorExtensionNotInstalledHex;
                    ButtonText = _localizationService.RetrieveString("Addon_BtnInstall");
                }
            });
        }
    }
}


