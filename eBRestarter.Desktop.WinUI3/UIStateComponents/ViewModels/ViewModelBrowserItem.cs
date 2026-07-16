using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.ObjectArchetypes.Models;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.SignalDTO.Messages;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelBrowserItem : ObservableObject
{
    private const string DownloadBrowserToggleButtonRedStyleKey = "DownloadBrowserToggleButtonRed";
    private const string DownloadBrowserToggleButtonStyleKey = "DownloadBrowserToggleButton";
    private const string InstalledIndicatorForegroundColorHex = "#7ED422";
    private const string InstallStateGlyphInstalledCharacter = "\uE73E";
    private const string InstallStateGlyphNotInstalledCharacter = "\uE711";
    private const string NotInstalledIndicatorForegroundColorHex = "#E40E87";

    private BrowserInfo _browserInfo;


    private readonly IDialogService _dialogService;

    private CancellationTokenSource? _downloadCancellationTokenSource;

    private readonly IUseCaseDownloadBrowser _downloadBrowserUseCase;

    private readonly IOutboundPortEVisitorConfigRepository _EVRestarterConfigRepository;

    private readonly IInboundPortLocalizationProvider _localizationService;

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
        ? _localizationService.RetrieveString("General_Cancel")
        : _localizationService.RetrieveString("General_Download");

    public string DownloadButtonStyleKey => IsDownloadActive
        ? DownloadBrowserToggleButtonRedStyleKey
        : DownloadBrowserToggleButtonStyleKey;

    public string HeaderImageBrowser => _browserInfo.IconPath;

    public string HeaderTitleBrowser => _browserInfo.Name;

    public static string ImageSizeHeightBrowser => "32";

    public static string ImageSizeWidthBrowser => "32";

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
        IUseCaseDownloadBrowser downloadBrowserUseCase,
        IOutboundPortEVisitorConfigRepository EVRestarterConfigRepository,
        IDialogService dialogService,
        IInboundPortLocalizationProvider LocalizationProvider)
    {
        ArgumentNullException.ThrowIfNull(browserInfo);
        ArgumentNullException.ThrowIfNull(downloadBrowserUseCase);
        ArgumentNullException.ThrowIfNull(EVRestarterConfigRepository);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);

        _browserInfo = browserInfo;
        _downloadBrowserUseCase = downloadBrowserUseCase;
        _EVRestarterConfigRepository = EVRestarterConfigRepository;
        _dialogService = dialogService;
        _localizationService = LocalizationProvider;

        BrowserVersionText = string.Empty;
        DownloadSizeText = string.Empty;
        RefreshBrowserVersionText();
    }

    [RelayCommand]
    private void ChooseBrowser()
    {
        var selectedBrowserName = HeaderTitleBrowser ?? string.Empty;
        var config = _EVRestarterConfigRepository.LoadConfig();
        config.Browser.Selected = selectedBrowserName;
        _EVRestarterConfigRepository.SaveConfig(config);

        WeakReferenceMessenger.Default.Send(new BrowserChangedMessage(selectedBrowserName));
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
            _localizationService.RetrieveString("Install_DialogTitle"),
            _localizationService.RetrieveString("Install_DialogQuestion"));

        if (installNow)
        {
            await _downloadBrowserUseCase.StartInstallerAsync(installerFilePath);
            await _dialogService.ShowMessageAsync(
                _localizationService.RetrieveString("Install_FinishedTitle"),
                _localizationService.RetrieveString("Install_FinishedMessage"));
        }
    }

    private void CancelDownload()
    {
        _downloadCancellationTokenSource?.Cancel();
        ResetDownloadState().Forget();
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
            string versionDisplayPrefix = _localizationService.RetrieveString("Browser_VersionPrefix");
            BrowserVersionText = $"{versionDisplayPrefix} {_browserInfo.Version}";
        }
        else if (IsDownloading)
        {
            BrowserVersionText = string.Empty;
        }
        else
        {
            BrowserVersionText = _localizationService.RetrieveString("Browser_NotInstalled");
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

        var progressHandler = new Progress<DownloadProgressStatus>(downloadProgressStatus =>
        {
            DownloadProgressValue = downloadProgressStatus.Percentage;
            DownloadSizeText =
                $"{FormatMegabytesFromBytes(downloadProgressStatus.BytesReceived):0.00} MB / {FormatMegabytesFromBytes(downloadProgressStatus.TotalBytes):0.00} MB";
        });

        try
        {
            var downloadPath = await _downloadBrowserUseCase.DownloadInstallerAsync(
                _browserInfo.Name,
                _browserInfo.DownloadUrl,
                progressHandler,
                _downloadCancellationTokenSource.Token);

            await AskToInstall(downloadPath);
        }
        catch (OperationCanceledException)
        {
            _downloadBrowserUseCase.CleanupPartialDownload(_browserInfo.Name);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine(ex);
            _downloadBrowserUseCase.CleanupPartialDownload(_browserInfo.Name);
        }
        finally
        {
            await ResetDownloadState();
            _downloadCancellationTokenSource?.Dispose();
            _downloadCancellationTokenSource = null;
        }
    }
}


