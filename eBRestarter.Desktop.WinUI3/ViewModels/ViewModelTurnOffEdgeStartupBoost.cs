using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Desktop.WinUI3.Enums;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelTurnOffEdgeStartupBoost : ObservableObject
{
    private const string EdgeStartupBoostSettingsClipboardText = "edge://settings/?search=Startup-Boost";

    private readonly IDialogService _dialogService;
    private readonly IInboundPortLocalizationProvider _localizationService;
    private readonly IUseCaseToggleEdgeStartupBoost _toggleEdgeStartupBoostUseCase;
    private readonly IOutboundPortSystemInfoProvider _windowsSystemInfo;

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
        IUseCaseToggleEdgeStartupBoost toggleEdgeStartupBoostUseCase,
        IDialogService dialogService,
        IInboundPortLocalizationProvider LocalizationProvider,
        IOutboundPortSystemInfoProvider windowsSystemInfo)
    {
        ArgumentNullException.ThrowIfNull(toggleEdgeStartupBoostUseCase);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);
        ArgumentNullException.ThrowIfNull(windowsSystemInfo);

        _toggleEdgeStartupBoostUseCase = toggleEdgeStartupBoostUseCase;
        _dialogService = dialogService;
        _localizationService = LocalizationProvider;
        _windowsSystemInfo = windowsSystemInfo;

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


