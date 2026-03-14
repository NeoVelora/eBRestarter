using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.UseCases.ToggleEdgeStartupBoost;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelTurnOffEdgeStartupBoost : ObservableObject
    {
        #region FieldsAndInjectedServices

        private readonly IToggleEdgeStartupBoostUseCase _toggleEdgeStartupBoostUseCase;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private bool _isRevertingState = false;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial bool IsStartupBoostEnabled { get; set; }
        [ObservableProperty] public partial bool IsInfoBarOpen { get; set; } = false;
        [ObservableProperty] public partial string InfoBarTitle { get; set; } = string.Empty;
        [ObservableProperty] public partial string InfoBarMessage { get; set; } = string.Empty;
        [ObservableProperty] public partial InfoBarSeverity InfoBarSeverity { get; set; } = InfoBarSeverity.Informational;

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelTurnOffEdgeStartupBoost(
            IToggleEdgeStartupBoostUseCase toggleEdgeStartupBoostUseCase,
            IDialogService dialogService,
            ILocalizationService localizationService)
        {
            _toggleEdgeStartupBoostUseCase = toggleEdgeStartupBoostUseCase;
            _dialogService = dialogService;
            _localizationService = localizationService;
            IsStartupBoostEnabled = _toggleEdgeStartupBoostUseCase.IsEnabled();
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task CopyAndOpenEdge()
        {
            if (_toggleEdgeStartupBoostUseCase.IsEdgeInstalled())
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText("edge://settings/?search=Startup-Boost");
                Clipboard.SetContent(dataPackage);

                await _dialogService.ShowMessageAsync(
                    "Edge",
                    _localizationService.GetString("StartupBoostDialog_CopyMessage"),
                    DialogIcon.Information);
            }
            else
            {
                await _dialogService.ShowMessageAsync(
                    "Edge",
                    _localizationService.GetString("Browser_NotInstalled"),
                    DialogIcon.Error);
            }
        }

        #endregion

        #region PropertyChangeHandlers

        async partial void OnIsStartupBoostEnabledChanged(bool value)
        {
            if (_isRevertingState) return;

            IsInfoBarOpen = false;
            var response = _toggleEdgeStartupBoostUseCase.Toggle(value);

            if (response.Success)
            {
                InfoBarTitle = _localizationService.GetString("StartupBoostDialog_SuccessTitle");
                InfoBarMessage = response.NewState
                    ? _localizationService.GetString("StartupBoostDialog_SuccessStatus_Activated")
                    : _localizationService.GetString("StartupBoostDialog_SuccessMessage");
                InfoBarSeverity = InfoBarSeverity.Success;
            }
            else
            {
                _isRevertingState = true;
                IsStartupBoostEnabled = response.NewState;
                _isRevertingState = false;

                InfoBarTitle = _localizationService.GetString("StartupBoostDialog_ErrorTitle");
                InfoBarMessage = response.ErrorMessage;
                InfoBarSeverity = InfoBarSeverity.Error;
            }

            IsInfoBarOpen = true;
        }

        #endregion
    }
}
