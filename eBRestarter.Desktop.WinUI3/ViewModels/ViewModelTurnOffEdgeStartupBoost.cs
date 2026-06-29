using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Providers;
using eBRestarter.Core.Application.Providers;
using eBRestarter.Core.Application.Ports.Outbound.SystemInfo;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.ToggleEdgeStartupBoost;
using eBRestarter.Core.Application.UseCases.ToggleEdgeStartupBoost;
using eBRestarter.Core.Application.Models.Errors;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Desktop.WinUI3.Models.Enums;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelTurnOffEdgeStartupBoost : ObservableObject
{
    private const string EdgeStartupBoostSettingsClipboardText = "edge://settings/?search=Startup-Boost";

    private readonly IDialogService _dialogService;
    private readonly ILocalizationProvider _localizationService;
    private readonly IToggleEdgeStartupBoostUseCase _toggleEdgeStartupBoostUseCase;
    private readonly IOsProcessControlPort _osProcessControlPort;
    private readonly IOsAutoLogonPort _osAutoLogonPort;
    private readonly ISystemInfoPort _windowsSystemInfo;
    private readonly ISettingsPort _settingsPort;
    private readonly IAutoStartPort _autoStartPort;
    private readonly IFileSystemPort _fileSystemPort;
    private readonly IBrowserConfigPort _browserConfigPort;

    private bool _isRevertingState;

    [ObservableProperty]
    public partial string InfoBarMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InfoBarTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsInfoBarOpen { get; set; }

    [ObservableProperty]
    public partial bool IsStartupBoostEnabled { get; set; }


    [ObservableProperty]
    public partial InfoBarSeverity InfoBarSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAdministrator))]
    public partial bool IsAdministrator { get; set; }

    public bool IsNotAdministrator => !IsAdministrator;

    public ViewModelTurnOffEdgeStartupBoost(
        IToggleEdgeStartupBoostUseCase toggleEdgeStartupBoostUseCase,
        IDialogService dialogService,
        ILocalizationProvider LocalizationProvider,
        IOsProcessControlPort osProcessControlPort, IOsAutoLogonPort osAutoLogonPort, ISystemInfoPort windowsSystemInfo, ISettingsPort settingsPort, IAutoStartPort autoStartPort, IFileSystemPort fileSystemPort, IBrowserConfigPort browserConfigPort)
    {
        ArgumentNullException.ThrowIfNull(toggleEdgeStartupBoostUseCase);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);
        ArgumentNullException.ThrowIfNull(osProcessControlPort);
        ArgumentNullException.ThrowIfNull(osAutoLogonPort);
        ArgumentNullException.ThrowIfNull(windowsSystemInfo);
        ArgumentNullException.ThrowIfNull(settingsPort);
        ArgumentNullException.ThrowIfNull(autoStartPort);
        ArgumentNullException.ThrowIfNull(fileSystemPort);
        ArgumentNullException.ThrowIfNull(browserConfigPort);

        _toggleEdgeStartupBoostUseCase = toggleEdgeStartupBoostUseCase;
        _dialogService = dialogService;
        _localizationService = LocalizationProvider;
        _osProcessControlPort = osProcessControlPort;
        _osAutoLogonPort = osAutoLogonPort;
        _windowsSystemInfo = windowsSystemInfo;
        _settingsPort = settingsPort;
        _autoStartPort = autoStartPort;
        _fileSystemPort = fileSystemPort;
        _browserConfigPort = browserConfigPort;

        IsAdministrator = _windowsSystemInfo.IsUserAdministrator();
        IsStartupBoostEnabled = _toggleEdgeStartupBoostUseCase.IsEnabled();
    }

    [RelayCommand]
    private async Task CopyAndOpenEdge()
    {
        string dialogTitle = _localizationService.RetrieveString("StartupBoostDialog.Title");

        if (_toggleEdgeStartupBoostUseCase.IsEdgeInstalled())
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(EdgeStartupBoostSettingsClipboardText);
            Clipboard.SetContent(dataPackage);

            await _dialogService.ShowMessageAsync(
                dialogTitle,
                _localizationService.RetrieveString("StartupBoostDialog_CopyMessage"),
                DialogIcon.Information);
        }
        else
        {
            await _dialogService.ShowMessageAsync(
                dialogTitle,
                _localizationService.RetrieveString("Browser_NotInstalled"),
                DialogIcon.Error);
        }
    }

    async partial void OnIsStartupBoostEnabledChanged(bool value)
    {
        if (_isRevertingState)
            return;

        IsInfoBarOpen = false;

        var toggleResponse = _toggleEdgeStartupBoostUseCase.Toggle(value);

        if (toggleResponse.Success)
        {
            InfoBarTitle = _localizationService.RetrieveString("StartupBoostDialog_SuccessTitle");
            InfoBarMessage = toggleResponse.NewState
                ? _localizationService.RetrieveString("StartupBoostDialog_SuccessStatus_Activated")
                : _localizationService.RetrieveString("StartupBoostDialog_SuccessMessage");
            InfoBarSeverity = InfoBarSeverity.Success;
        }
        else
        {
            _isRevertingState = true;
            IsStartupBoostEnabled = toggleResponse.NewState;
            _isRevertingState = false;

            InfoBarTitle = _localizationService.RetrieveString("StartupBoostDialog_ErrorTitle");
            InfoBarMessage = toggleResponse.ErrorMessage ?? string.Empty;
            InfoBarSeverity = InfoBarSeverity.Error;
        }

        IsInfoBarOpen = true;
    }
}


