using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Models;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Core.Domain.Models.Records.Config;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class BrowserItemViewModel : ObservableObject
    {
        private readonly BrowserInfo _browserInfo;

        private readonly AppConfig _currentConfig;

        private readonly IBrowserDownloadService _downloadService;

        private readonly IOperatingSystemFacade _os;

        private readonly IEVisitorConfigService _eVisitorConfigService;

        private CancellationTokenSource? _cts;
        public BrowserItemViewModel(
            BrowserInfo info,
            IBrowserDownloadService downloadService,
            IOperatingSystemFacade os,
            IEVisitorConfigService eVisitorConfigService)
        {
            _browserInfo = info;
            _eVisitorConfigService = eVisitorConfigService;
            _downloadService = downloadService;
            _os = os;

            _currentConfig = _eVisitorConfigService.LoadConfig();
        }
        private const string SetForegroundColorGreen = "#7ED422";
        private const string SetForegroundColorRed = "#E40E87";
        public string HeaderTitleBrowser => _browserInfo.Name;
        public string HeaderImageBrowser => _browserInfo.IconPath;

        public string ImageSizeHeightBrowser => _browserInfo.IconHeight;
        public string ImageSizeWidthBrowser => _browserInfo.IconWidth;

        public string BrowserVersion => _browserInfo.IsInstalled ? $"Version: {_browserInfo.Version}" : "Nicht installiert";

        public SolidColorBrush BrowserExistTextForground => _browserInfo.IsInstalled ?
            new SolidColorBrush(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(SetForegroundColorGreen)) :
            new SolidColorBrush(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(SetForegroundColorRed));
        public string BrowserExist => _browserInfo.IsInstalled ? "✓" : "✘";

        public bool IsDownloadButtonVisible => !_browserInfo.IsInstalled;
        public bool IsChooseButtonVisible => _browserInfo.IsInstalled;

        [RelayCommand]
        private void Download() { }

        [RelayCommand]
        private void ChooseBrowser() {

            _currentConfig.Browser.Selected = HeaderTitleBrowser;
            SaveSettings();

        }


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DownloadButtonContent))]
        [NotifyPropertyChangedFor(nameof(DownloadButtonStyleKey))]
        public partial bool IsDownloadActive { get; set; }

        public string DownloadButtonContent => IsDownloadActive ? "Abbrechen" : "Download";

        public bool IsDownloading => IsDownloadActive;
        public bool IsNotDownloading => !IsDownloadActive;

        [ObservableProperty]
        public partial double DownloadProgressValue { get; set; }

        [ObservableProperty]
        public partial string DownloadSizeText { get; set; } = string.Empty;


        public string DownloadButtonStyleKey => IsDownloadActive ? "DownloadBrowserToggleButtonRed" : "DownloadBrowserToggleButton";


        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task ToggleDownload()
        {
            if (IsDownloadActive)
            {
                CancelDownload();
            }
            else
            {
                await StartDownloadAsync();
            }
        }

        private void CancelDownload()
        {
            _cts?.Cancel();
            ResetDownloadState();
        }

        private async Task StartDownloadAsync()
        {
            _cts = new CancellationTokenSource();

            IsDownloadActive = true;

            var fileName = $"{_browserInfo.Name}_Installer.exe";
            var downloadPath = _os.WindowsFileSystemService.CombinePaths(
                _os.WindowsFileSystemService.GetEnvironmentPath("UserProfile"), "Downloads", fileName);

            var progressHandler = new Progress<DownloadProgressStatus>(status =>
            {
                DownloadProgressValue = status.Percentage;
                DownloadSizeText = $"{status.BytesReceived / 1024d / 1024d:0.00} MB / {status.TotalBytes / 1024d / 1024d:0.00} MB";
            });

            try
            {
                await _downloadService.DownloadFileAsync(_browserInfo.DownloadUrl, downloadPath, progressHandler, _cts.Token);

                ResetDownloadState();
                AskToInstall(downloadPath);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ResetDownloadState();
            }
        }

        private void ResetDownloadState()
        {
            IsDownloadActive = false;
            DownloadProgressValue = 0;
            DownloadSizeText = "";
            _cts = null;
        }

        private void AskToInstall(string path)
        {
        }

        private void SaveSettings()
        {
            _eVisitorConfigService.SaveConfig(_currentConfig);
        }
    }
}
