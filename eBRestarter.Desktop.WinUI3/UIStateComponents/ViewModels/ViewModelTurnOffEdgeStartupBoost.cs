using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.Enums;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelTurnOffEdgeStartupBoost : ObservableObject
{
    private const string BrowserNotInstalledResourceKey = "Browser_NotInstalled";
    private const string EdgeStartupBoostSettingsClipboardText = "edge://settings/?search=Startup-Boost";
    private const string StartupBoostDialogCopyMessageResourceKey = "StartupBoostDialog_CopyMessage";
    private const string StartupBoostDialogErrorTitleResourceKey = "StartupBoostDialog_ErrorTitle";
    private const string StartupBoostDialogSuccessMessageResourceKey = "StartupBoostDialog_SuccessMessage";
    private const string StartupBoostDialogSuccessStatusActivatedResourceKey = "StartupBoostDialog_SuccessStatus_Activated";
    private const string StartupBoostDialogSuccessTitleResourceKey = "StartupBoostDialog_SuccessTitle";
    private const string StartupBoostDialogTitleResourceKey = "StartupBoostDialog.Title";

    private readonly IDialogService _dialogService;
    private readonly IInboundPortLocalizationProvider _localizationService;
    private readonly IUseCaseToggleEdgeStartupBoost _toggleEdgeStartupBoostUseCase;
    private readonly IOutboundPortSystemInfoProvider _windowsSystemInfo;

    private bool _isRevertingState;

    [ObservableProperty] public partial string InfoBarMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial string InfoBarTitle { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAdministrator))]
    public partial bool IsAdministrator { get; set; }

    [ObservableProperty] public partial bool IsInfoBarOpen { get; set; }
    [ObservableProperty] public partial bool IsStartupBoostEnabled { get; set; }

    async partial void OnIsStartupBoostEnabledChanged(bool value)
    {
        if (_isRevertingState)
        {
            return;
        }

        IsInfoBarOpen = false;

        var toggleResponse = _toggleEdgeStartupBoostUseCase.Toggle(value);

        if (toggleResponse.Success)
        {
            InfoBarTitle = _localizationService.RetrieveString(StartupBoostDialogSuccessTitleResourceKey);
            InfoBarMessage = toggleResponse.NewState
                ? _localizationService.RetrieveString(StartupBoostDialogSuccessStatusActivatedResourceKey)
                : _localizationService.RetrieveString(StartupBoostDialogSuccessMessageResourceKey);
            InfoBarSeverity = InfoBarSeverity.Success;
        }
        else
        {
            _isRevertingState = true;
            IsStartupBoostEnabled = toggleResponse.NewState;
            _isRevertingState = false;

            InfoBarTitle = _localizationService.RetrieveString(StartupBoostDialogErrorTitleResourceKey);
            InfoBarMessage = toggleResponse.ErrorMessage ?? string.Empty;
            InfoBarSeverity = InfoBarSeverity.Error;
        }

        IsInfoBarOpen = true;
    }

    [ObservableProperty] public partial InfoBarSeverity InfoBarSeverity { get; set; } = InfoBarSeverity.Informational;


    public bool IsNotAdministrator => !IsAdministrator;

    public ViewModelTurnOffEdgeStartupBoost(
        IDialogService dialogService,
        IInboundPortLocalizationProvider localizationService,
        IUseCaseToggleEdgeStartupBoost toggleEdgeStartupBoostUseCase,
        IOutboundPortSystemInfoProvider windowsSystemInfo)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(toggleEdgeStartupBoostUseCase);
        ArgumentNullException.ThrowIfNull(windowsSystemInfo);

        _dialogService = dialogService;
        _localizationService = localizationService;
        _toggleEdgeStartupBoostUseCase = toggleEdgeStartupBoostUseCase;
        _windowsSystemInfo = windowsSystemInfo;

        IsAdministrator = _windowsSystemInfo.IsUserAdministrator();
        IsStartupBoostEnabled = _toggleEdgeStartupBoostUseCase.IsEnabled();
    }

    [RelayCommand]
    private async Task CopyAndOpenEdgeAsync()
    {
        string dialogTitle = _localizationService.RetrieveString(StartupBoostDialogTitleResourceKey);

        if (_toggleEdgeStartupBoostUseCase.IsEdgeInstalled())
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(EdgeStartupBoostSettingsClipboardText);
            Clipboard.SetContent(dataPackage);

            await _dialogService.ShowMessageAsync(
                dialogTitle,
                _localizationService.RetrieveString(StartupBoostDialogCopyMessageResourceKey),
                DialogIcon.Information);
        }
        else
        {
            await _dialogService.ShowMessageAsync(
                dialogTitle,
                _localizationService.RetrieveString(BrowserNotInstalledResourceKey),
                DialogIcon.Error);
        }
    }
}
