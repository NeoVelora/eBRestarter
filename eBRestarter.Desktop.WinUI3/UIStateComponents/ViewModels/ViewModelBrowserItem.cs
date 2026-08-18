using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Media;

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
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.SignalDTO.Messages;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelBrowserItem : ObservableObject
{
    private const string DownloadBrowserToggleButtonRedStyleKey = "DownloadBrowserToggleButtonRed";
    private const string DownloadBrowserToggleButtonStyleKey = "DownloadBrowserToggleButton";
    private const string ImageSizeHeightBrowserValue = "32";
    private const string ImageSizeWidthBrowserValue = "32";
    private const string InstalledIndicatorForegroundColorHex = "#7ED422";
    private const string InstallStateGlyphInstalledCharacter = "\uE73E";
    private const string InstallStateGlyphNotInstalledCharacter = "\uE711";
    private const string KeyBrowserNotInstalled = "Browser_NotInstalled";
    private const string KeyBrowserVersionPrefix = "Browser_VersionPrefix";
    private const string KeyGeneralCancel = "General_Cancel";
    private const string KeyGeneralDownload = "General_Download";
    private const string KeyInstallDialogQuestion = "Install_DialogQuestion";
    private const string KeyInstallDialogTitle = "Install_DialogTitle";
    private const string KeyInstallFinishedMessage = "Install_FinishedMessage";
    private const string KeyInstallFinishedTitle = "Install_FinishedTitle";
    private const string NotInstalledIndicatorForegroundColorHex = "#E40E87";

    private readonly IDialogService _dialogService;
    private readonly ILogger<ViewModelBrowserItem> _logger;
    private readonly IUseCaseDownloadBrowser _downloadBrowserUseCase;
    private readonly IOutboundPortEVisitorConfigRepository _evRestarterConfigRepository;
    private readonly IInboundPortLocalizationProvider _localizationService;

    private BrowserInfo _browserInfo;
    private CancellationTokenSource? _downloadCancellationTokenSource;
    private static readonly SolidColorBrush InstalledBrush = new(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(InstalledIndicatorForegroundColorHex));
    private static readonly SolidColorBrush NotInstalledBrush = new(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(NotInstalledIndicatorForegroundColorHex));


    public ViewModelBrowserItem(
        BrowserInfo browserInfo,
        IUseCaseDownloadBrowser downloadBrowserUseCase,
        IOutboundPortEVisitorConfigRepository evRestarterConfigRepository,
        IDialogService dialogService,
        IInboundPortLocalizationProvider localizationService,
        ILogger<ViewModelBrowserItem> logger)
    {
        ArgumentNullException.ThrowIfNull(browserInfo);
        ArgumentNullException.ThrowIfNull(downloadBrowserUseCase);
        ArgumentNullException.ThrowIfNull(evRestarterConfigRepository);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _browserInfo = browserInfo;
        _downloadBrowserUseCase = downloadBrowserUseCase;
        _evRestarterConfigRepository = evRestarterConfigRepository;
        _dialogService = dialogService;
        _localizationService = localizationService;

        BrowserVersionText = string.Empty;
        DownloadSizeText = string.Empty;
        RefreshBrowserVersionText();
    }


    public BrowserType BrowserType => _browserInfo.Type;

    [ObservableProperty]
    public partial string BrowserVersionText { get; set; }

    public string DownloadButtonContent => IsDownloadActive
        ? _localizationService.RetrieveString(KeyGeneralCancel)
        : _localizationService.RetrieveString(KeyGeneralDownload);

    public string DownloadButtonStyleKey => IsDownloadActive
        ? DownloadBrowserToggleButtonRedStyleKey
        : DownloadBrowserToggleButtonStyleKey;

    [ObservableProperty]
    public partial double DownloadProgressValue { get; set; }

    [ObservableProperty]
    public partial string DownloadSizeText { get; set; }

    public string HeaderImageBrowser => _browserInfo.IconPath;

    public string HeaderTitleBrowser => _browserInfo.Name;

    public static string ImageSizeHeightBrowser => ImageSizeHeightBrowserValue;

    public static string ImageSizeWidthBrowser => ImageSizeWidthBrowserValue;

    public string InstallStateGlyph => _browserInfo.IsInstalled
        ? InstallStateGlyphInstalledCharacter
        : InstallStateGlyphNotInstalledCharacter;

    public SolidColorBrush InstallStateGlyphForeground => _browserInfo.IsInstalled ? InstalledBrush : NotInstalledBrush;

    public bool IsChooseButtonVisible => _browserInfo.IsInstalled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DownloadButtonContent))]
    [NotifyPropertyChangedFor(nameof(DownloadButtonStyleKey))]
    [NotifyPropertyChangedFor(nameof(IsDownloading))]
    [NotifyPropertyChangedFor(nameof(IsNotDownloading))]
    public partial bool IsDownloadActive { get; set; }

    public bool IsDownloadButtonVisible => !_browserInfo.IsInstalled;

    public bool IsDownloading => IsDownloadActive;

    public bool IsDownloadSizeTextVisible => !_browserInfo.IsInstalled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotInstalling))]
    public partial bool IsInstalling { get; set; }

    public bool IsNotDownloading => !IsDownloadActive;

    public bool IsNotInstalling => !IsInstalling;


    private async Task AskToInstallAsync(string installerFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerFilePath);

        bool installNow = await _dialogService.ShowYesNoDialogAsync(
            _localizationService.RetrieveString(KeyInstallDialogTitle),
            _localizationService.RetrieveString(KeyInstallDialogQuestion));

        if (installNow)
        {
            await _downloadBrowserUseCase.StartInstallerAsync(installerFilePath);
            await _dialogService.ShowMessageAsync(
                _localizationService.RetrieveString(KeyInstallFinishedTitle),
                _localizationService.RetrieveString(KeyInstallFinishedMessage));
        }
    }

    private void CancelDownload()
    {
        _downloadCancellationTokenSource?.Cancel();
        ResetDownloadState();
    }

    [RelayCommand]
    private void ChooseBrowser()
    {
        var selectedBrowserName = HeaderTitleBrowser ?? string.Empty;
        var config = _evRestarterConfigRepository.LoadConfig();
        if (config?.Browser != null)
        {
            config.Browser.Selected = selectedBrowserName;
            _evRestarterConfigRepository.SaveConfig(config);
        }

        WeakReferenceMessenger.Default.Send(new BrowserChangedMessage(selectedBrowserName));
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
            string versionDisplayPrefix = _localizationService.RetrieveString(KeyBrowserVersionPrefix);
            BrowserVersionText = $"{versionDisplayPrefix} {_browserInfo.Version}";
        }
        else if (IsDownloading)
        {
            BrowserVersionText = string.Empty;
        }
        else
        {
            BrowserVersionText = _localizationService.RetrieveString(KeyBrowserNotInstalled);
        }
    }

    private void ResetDownloadState()
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
            _logger.LogWarning(
                LogEventIds.Update.UpdateDownloadUrlMissing,
                "No download URL is configured for {BrowserName}; the download was not started.",
                _browserInfo.Name);

            _downloadCancellationTokenSource.Dispose();
            _downloadCancellationTokenSource = null;

            return;
        }

        IsDownloadActive = true;
        RefreshBrowserVersionText();

        var progressHandler = new Progress<DownloadProgressStatus>(downloadProgressStatus =>
        {
            DownloadProgressValue = downloadProgressStatus.Percentage;
            DownloadSizeText = $"{FormatMegabytesFromBytes(downloadProgressStatus.BytesReceived):0.00} MB / {FormatMegabytesFromBytes(downloadProgressStatus.TotalBytes):0.00} MB";
        });

        try
        {
            var downloadPath = await _downloadBrowserUseCase.DownloadInstallerAsync(
                _browserInfo.Name,
                _browserInfo.DownloadUrl,
                progressHandler,
                _downloadCancellationTokenSource.Token);

            await AskToInstallAsync(downloadPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                LogEventIds.Update.BrowserInstallerCleanupFailed,
                "Download of {BrowserName} was cancelled by the user; cleaning up the partial file.",
                _browserInfo.Name);

            _downloadBrowserUseCase.CleanupPartialDownload(_browserInfo.Name);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(
                LogEventIds.Update.BrowserInstallerCleanupFailed,
                ex,
                "Downloading or installing {BrowserName} failed.",
                _browserInfo.Name);
            _downloadBrowserUseCase.CleanupPartialDownload(_browserInfo.Name);
        }
        finally
        {
            ResetDownloadState();
            _downloadCancellationTokenSource?.Dispose();
            _downloadCancellationTokenSource = null;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ToggleDownloadAsync()
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

    public void Update(BrowserInfo updatedBrowserInfo)
    {
        ArgumentNullException.ThrowIfNull(updatedBrowserInfo);

        if (_browserInfo.IsInstalled == updatedBrowserInfo.IsInstalled && _browserInfo.Version == updatedBrowserInfo.Version)
        {
            return;
        }

        _browserInfo = updatedBrowserInfo;
        NotifyBrowserInstallSnapshotChanged();
    }
}
