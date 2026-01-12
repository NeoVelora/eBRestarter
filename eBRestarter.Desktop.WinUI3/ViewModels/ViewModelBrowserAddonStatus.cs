using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelBrowserAddonStatus : ObservableObject
    {
        private readonly IBrowser _browser;
        private readonly DispatcherQueue _dispatcherQueue;

        [ObservableProperty] public partial string BrowserName { get; set; } = string.Empty;
        [ObservableProperty] public partial string IconPath { get; set; } = string.Empty;

        [ObservableProperty] public partial string InstallStatusText { get; set; } = "Prüfe...";
        [ObservableProperty] public partial string InstallStatusColor { get; set; } = "#000000";

        [ObservableProperty] public partial string AddonStatusText { get; set; } = "Prüfe...";
        [ObservableProperty] public partial string AddonStatusColor { get; set; } = "#000000";
        [ObservableProperty] public partial string AddonStatusIcon { get; set; } = "?";

        [ObservableProperty] public partial string ButtonText { get; set; } = "Lade...";
        [ObservableProperty] public partial bool IsButtonEnabled { get; set; } = false;

        public ViewModelBrowserAddonStatus(IBrowser browser)
        {
            _browser = browser;

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            BrowserName = _browser.DisplayName;

            IconPath = browser.IconPath.Replace("/Resources/Visuals", "ms-appx:///Assets/Visuals");

            RefreshStatus();
        }

        public void RefreshStatus()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (_browser.IsInstalled)
                {
                    InstallStatusText = $"{BrowserName} ist installiert";
                    InstallStatusColor = "{ThemeResource TextFillColorPrimaryBrush}";
                    IsButtonEnabled = true;

                    bool hasAddon = _browser.IsExtensionInstalled(null);

                    if (hasAddon)
                    {
                        AddonStatusText = "Add-on ist installiert";
                        AddonStatusIcon = "✓";
                        AddonStatusColor = "#7ED422";
                        ButtonText = "Add-on verwalten";
                    }
                    else
                    {
                        AddonStatusText = "Add-on ist nicht installiert";
                        AddonStatusIcon = "✘";
                        AddonStatusColor = "#E40E87";
                        ButtonText = "Add-on installieren";
                    }
                }
                else
                {
                    InstallStatusText = $"{BrowserName} ist nicht installiert";
                    InstallStatusColor = "#BA224D";

                    AddonStatusText = "-";
                    AddonStatusIcon = "";
                    IsButtonEnabled = false;
                    ButtonText = "Browser fehlt";
                }
            });
        }

        [RelayCommand]
        private void OpenStore()
        {
            if (!string.IsNullOrEmpty(_browser.ExtensionInstallUrl))
            {
                _browser.Start(_browser.ExtensionInstallUrl);
            }
        }
    }
}
