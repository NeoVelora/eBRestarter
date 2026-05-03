using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Extensions;
using eBRestarter.Core.Application.Models;
using eBRestarter.Core.Application.Models.Config;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Desktop.WinUI3.Messages;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public partial class ViewModelBrowserItem : ObservableObject
{
    private const string BrowserInstallerFileNameSuffix = "_Installer.exe";

    private const string DownloadBrowserToggleButtonRedStyleKey = "DownloadBrowserToggleButtonRed";

    private const string DownloadBrowserToggleButtonStyleKey = "DownloadBrowserToggleButton";

    private const string InstalledIndicatorForegroundColorHex = "#7ED422";

    private const string InstallStateGlyphInstalledCharacter = "\u2713";

    private const string InstallStateGlyphNotInstalledCharacter = "\u2717";

    private const string NotInstalledIndicatorForegroundColorHex = "#E40E87";

    private const string UserDownloadsFolderName = "Downloads";

    private const string UserProfileEnvironmentVariableName = "UserProfile";

    private BrowserInfo _browserInfo;

    private readonly AppConfig _currentConfig;

    private readonly IDialogService _dialogService;

    private CancellationTokenSource? _downloadCancellationTokenSource;

    private readonly IBrowserDownloadService _downloadService;

    private readonly IEVisitorConfigService _eVisitorConfigService;

    private readonly ILocalizationService _localizationService;

    private readonly IOperatingSystemFacade _operatingSystemFacade;

    [ObservableProperty]
    public partial double DownloadProgressValue { get; set; }


    [ObservableProperty]
    public partial string BrowserVersionText { get; set; }

    [ObservableProperty]
    public partial string DownloadSizeText { get; set; }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DownloadButtonContent))]
    [NotifyPropertyChangedFor(nameof(DownloadButtonStyleKey))]
    [NotifyPropertyChangedFor(nameof(IsDownloading))]
    [NotifyPropertyChangedFor(nameof(IsNotDownloading))]
    public partial bool IsDownloadActive { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotInstalling))]
    public partial bool IsInstalling { get; set; }


    public SolidColorBrush InstallStateGlyphForeground => _browserInfo.IsInstalled
        ? new SolidColorBrush(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(InstalledIndicatorForegroundColorHex))
        : new SolidColorBrush(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(NotInstalledIndicatorForegroundColorHex));


    public string DownloadButtonContent => IsDownloadActive
        ? _localizationService.GetString("General_Cancel")
        : _localizationService.GetString("General_Download");

    public string DownloadButtonStyleKey => IsDownloadActive
        ? DownloadBrowserToggleButtonRedStyleKey
        : DownloadBrowserToggleButtonStyleKey;

    public string HeaderImageBrowser => _browserInfo.IconPath;

    public string HeaderTitleBrowser => _browserInfo.Name;

    public string ImageSizeHeightBrowser => _browserInfo.IconHeight;

    public string ImageSizeWidthBrowser => _browserInfo.IconWidth;

    public string InstallStateGlyph => _browserInfo.IsInstalled
        ? InstallStateGlyphInstalledCharacter
        : InstallStateGlyphNotInstalledCharacter;


    public bool IsChooseButtonVisible => _browserInfo.IsInstalled;

    public bool IsDownloadButtonVisible => !_browserInfo.IsInstalled;

    public bool IsDownloadSizeTextVisible => !_browserInfo.IsInstalled;

    public bool IsDownloading => IsDownloadActive;

    public bool IsNotDownloading => !IsDownloadActive;

    public bool IsNotInstalling => !IsInstalling;


    public BrowserType BrowserType => _browserInfo.Type;

    public ViewModelBrowserItem(
        BrowserInfo browserInfo,
        IBrowserDownloadService downloadService,
        IOperatingSystemFacade operatingSystemFacade,
        IEVisitorConfigService eVisitorConfigService,
        IDialogService dialogService,
        ILocalizationService localizationService)
    {
        ArgumentNullException.ThrowIfNull(browserInfo);
        ArgumentNullException.ThrowIfNull(downloadService);
        ArgumentNullException.ThrowIfNull(operatingSystemFacade);
        ArgumentNullException.ThrowIfNull(eVisitorConfigService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(localizationService);

        _browserInfo = browserInfo;
        _downloadService = downloadService;
        _operatingSystemFacade = operatingSystemFacade;
        _eVisitorConfigService = eVisitorConfigService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _currentConfig = _eVisitorConfigService.LoadConfig();
        BrowserVersionText = string.Empty;
        DownloadSizeText = string.Empty;
        RefreshBrowserVersionText();
    }

    [RelayCommand]
    private void ChooseBrowser()
    {
        var selectedBrowserName = HeaderTitleBrowser ?? string.Empty;
        _currentConfig.Browser.Selected = selectedBrowserName;
        WeakReferenceMessenger.Default.Send(new BrowserChangedMessage(selectedBrowserName));
        SaveSettings();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ToggleDownload()
    {
        if (IsDownloadActive)
            CancelDownload();
        else
            await StartDownloadAsync();
    }

    public void Update(BrowserInfo updatedBrowserInfo)
    {
        ArgumentNullException.ThrowIfNull(updatedBrowserInfo);

        if (_browserInfo.IsInstalled == updatedBrowserInfo.IsInstalled && _browserInfo.Version == updatedBrowserInfo.Version)
            return;

        _browserInfo = updatedBrowserInfo;
        NotifyBrowserInstallSnapshotChanged();
    }

    private async Task AskToInstall(string installerFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerFilePath);

        bool installNow = await _dialogService.ShowYesNoDialogAsync(
            _localizationService.GetString("Install_DialogTitle"),
            _localizationService.GetString("Install_DialogQuestion"));

        if (installNow)
        {
            await _operatingSystemFacade.WindowsProcessControlService.StartExecutableAsync(installerFilePath);
            await _dialogService.ShowMessageAsync(
                _localizationService.GetString("Install_FinishedTitle"),
                _localizationService.GetString("Install_FinishedMessage"));
        }
    }

    private void CancelDownload()
    {
        _downloadCancellationTokenSource?.Cancel();
        ResetDownloadState().Forget();
    }

    private void CleanupPartialFile(string partialDownloadFilePath)
    {
        try
        {
            if (_operatingSystemFacade.WindowsFileSystemService.FileExists(partialDownloadFilePath))
                _operatingSystemFacade.WindowsFileSystemService.DeleteFile(partialDownloadFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            Debug.WriteLine(ex);
        }
    }

    private static double FormatMegabytesFromBytes(long byteCount) =>
        byteCount / (1024d * 1024d);

    private void NotifyBrowserInstallSnapshotChanged()
    {
        OnPropertyChanged(nameof(InstallStateGlyph));
        OnPropertyChanged(nameof(InstallStateGlyphForeground));
        RefreshBrowserVersionText();
        OnPropertyChanged(nameof(IsChooseButtonVisible));
        OnPropertyChanged(nameof(IsDownloadButtonVisible));
        OnPropertyChanged(nameof(IsDownloadSizeTextVisible));
        OnPropertyChanged(nameof(DownloadButtonContent));
    }

    private void RefreshBrowserVersionText()
    {
        if (_browserInfo.IsInstalled && !IsDownloading)
        {
            string versionDisplayPrefix = _localizationService.GetString("Browser_VersionPrefix");
            BrowserVersionText = $"{versionDisplayPrefix} {_browserInfo.Version}";
        }
        else if (IsDownloading)
        {
            BrowserVersionText = string.Empty;
        }
        else
        {
            BrowserVersionText = _localizationService.GetString("Browser_NotInstalled");
        }
    }

    private async Task ResetDownloadState()
    {
        IsDownloadActive = false;
        DownloadProgressValue = 0;
        DownloadSizeText = string.Empty;
        RefreshBrowserVersionText();
        _downloadCancellationTokenSource = null;
    }

    private void SaveSettings()
    {
        _eVisitorConfigService.SaveConfig(_currentConfig);
    }

    private async Task StartDownloadAsync()
    {
        _downloadCancellationTokenSource?.Dispose();
        _downloadCancellationTokenSource = new CancellationTokenSource();

        if (string.IsNullOrWhiteSpace(_browserInfo.DownloadUrl))
        {
            Debug.WriteLine($"{nameof(ViewModelBrowserItem)}: {nameof(BrowserInfo.DownloadUrl)} is missing; download was not started.");
            _downloadCancellationTokenSource.Dispose();
            _downloadCancellationTokenSource = null;
            return;
        }

        IsDownloadActive = true;
        RefreshBrowserVersionText();

        var fileName = $"{_browserInfo.Name}{BrowserInstallerFileNameSuffix}";
        var userProfile = _operatingSystemFacade.WindowsFileSystemService.GetEnvironmentPath(UserProfileEnvironmentVariableName);
        var downloadPath = _operatingSystemFacade.WindowsFileSystemService.CombinePaths(userProfile, UserDownloadsFolderName, fileName);

        var progressHandler = new Progress<DownloadProgressStatus>(downloadProgressStatus =>
        {
            DownloadProgressValue = downloadProgressStatus.Percentage;
            DownloadSizeText =
                $"{FormatMegabytesFromBytes(downloadProgressStatus.BytesReceived):0.00} MB / {FormatMegabytesFromBytes(downloadProgressStatus.TotalBytes):0.00} MB";
        });

        try
        {
            await _downloadService.DownloadFileAsync(
                _browserInfo.DownloadUrl,
                downloadPath,
                progressHandler,
                _downloadCancellationTokenSource.Token);
            await AskToInstall(downloadPath);
        }
        catch (OperationCanceledException)
        {
            CleanupPartialFile(downloadPath);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine(ex);
            CleanupPartialFile(downloadPath);
        }
        finally
        {
            await ResetDownloadState();
            _downloadCancellationTokenSource?.Dispose();
            _downloadCancellationTokenSource = null;
        }
    }
}
