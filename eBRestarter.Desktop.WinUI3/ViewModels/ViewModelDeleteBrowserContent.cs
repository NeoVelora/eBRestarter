using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.UseCases.DeleteBrowserContent;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelDeleteBrowserContent : ObservableObject
    {
        #region FieldsAndInjectedServices

        private readonly IDeleteBrowserContentUseCase _deleteBrowserContentUseCase;
        private readonly IBrowserFactory _browserFactory;
        private readonly ILocalizationService _localizationService;
        private readonly IEVisitorConfigService _eVisitorConfigService;

        private CancellationTokenSource? _cts;
        private bool _isAutoMode = false;
        private BrowserType _selectedBrowserType;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial string BrowserIconPath { get; set; } = string.Empty;
        [ObservableProperty] public partial string BrowserName { get; set; }
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelCleaningCommand))]
        public partial bool IsBusy { get; set; } = false;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
        public partial bool IsDeleteCookiesChecked { get; set; } = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
        public partial bool IsDeleteInternetCacheChecked { get; set; } = true;
        [ObservableProperty] public partial bool IsProcessConflict { get; set; } = false;
        [ObservableProperty] public partial double ProgressMaximum { get; set; } = 100;
        [ObservableProperty] public partial string ProgressText { get; set; } = "0 %";
        [ObservableProperty] public partial double ProgressValue { get; set; } = 0;
        [ObservableProperty] public partial string StatusText { get; set; }

        #endregion

        #region PublicProperties

        public event Action? RequestCloseDialog;

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelDeleteBrowserContent(
            IDeleteBrowserContentUseCase deleteBrowserContentUseCase,
            IBrowserFactory browserFactory,
            IEVisitorConfigService eVisitorConfigService,
            ILocalizationService localizationService)
        {
            _deleteBrowserContentUseCase = deleteBrowserContentUseCase;
            _browserFactory = browserFactory;
            _eVisitorConfigService = eVisitorConfigService;
            _localizationService = localizationService;

            BrowserName = _localizationService.GetString("Cleanup_Loading");
            StatusText = _localizationService.GetString("Cleanup_Ready");

            AppConfig appConfig = eVisitorConfigService.LoadConfig();
            string selectedBrowserString = appConfig.Browser.Selected;

            if (Enum.TryParse(typeof(BrowserType), selectedBrowserString, true, out var parsedResult))
            {
                var browserType = (BrowserType)parsedResult;
                Initialize(browserType);
            }
            else
            {
                string errorFormat = _localizationService.GetString("Cleanup_ConfigError");
                StatusText = string.Format(errorFormat, selectedBrowserString);
            }
        }

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanCancel))]
        private void CancelCleaning()
        {
            _cts?.Cancel();
            StatusText = _localizationService.GetString("Cleanup_Canceling");
        }

        [RelayCommand(CanExecute = nameof(CanClean))]
        private async Task StartCleaning()
        {
            if (IsBusy) return;
            await ExecuteCleaningLogic(false);
        }

        [RelayCommand]
        private async Task ForceCloseAndContinue()
        {
            IsProcessConflict = false;
            await ExecuteCleaningLogic(true);
        }

        [RelayCommand]
        private void CancelConflict()
        {
            IsProcessConflict = false;
            StatusText = _localizationService.GetString("Cleanup_CanceledByUser");
        }

        #endregion

        #region PublicAndProtectedMethods

        public async Task RunAutoSequenceAsync()
        {
            _isAutoMode = true;
            await Task.Delay(500);
            await StartCleaning();
        }

        public void Initialize(BrowserType selectedBrowserType)
        {
            try
            {
                _selectedBrowserType = selectedBrowserType;
                var currentBrowser = _browserFactory.Create(selectedBrowserType);
                BrowserName = currentBrowser.DisplayName;
                BrowserIconPath = currentBrowser.IconPath;
            }
            catch (Exception ex)
            {
                string errorFormat = _localizationService.GetString("General_LoadErrorPrefix");
                StatusText = string.Format(errorFormat, ex.Message);
            }
        }

        #endregion

        #region PrivateHelperMethods

        private bool CanCancel() => IsBusy;
        private bool CanClean() => !IsBusy && (IsDeleteCookiesChecked || IsDeleteInternetCacheChecked);

        private async Task ExecuteCleaningLogic(bool forceClose)
        {
            IsBusy = true;
            _cts = new CancellationTokenSource();

            try
            {
                var request = new DeleteBrowserContentRequest(
                    BrowserType: _selectedBrowserType,
                    DeleteCookies: IsDeleteCookiesChecked,
                    DeleteCache: IsDeleteInternetCacheChecked,
                    ForceCloseProcess: forceClose
                );

                var progress = new Progress<DeleteBrowserContentProgress>(p =>
                {
                    StatusText = p.StatusMessage;
                    ProgressMaximum = p.TotalFiles > 0 ? p.TotalFiles : 1;
                    ProgressValue = p.CurrentFile;
                    if (p.TotalFiles > 0)
                    {
                        ProgressText = $"{p.CurrentFile * 100 / p.TotalFiles} %";
                    }
                });

                var response = await _deleteBrowserContentUseCase.ExecuteAsync(request, progress, _cts.Token);

                if (response.ProcessConflict)
                {
                    IsProcessConflict = true;
                }
                else if (!response.Success)
                {
                    StatusText = response.ErrorMessage;
                }
                else
                {
                    if (_isAutoMode)
                    {
                        await Task.Delay(1000);
                        RequestCloseDialog?.Invoke();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                StatusText = _localizationService.GetString("Cleanup_CanceledByUser");
            }
            catch (Exception ex)
            {
                string errorFormat = _localizationService.GetString("General_ErrorPrefix");
                StatusText = string.Format(errorFormat, ex.Message);
            }
            finally
            {
                IsBusy = false;
                _cts = null;
            }
        }

        #endregion
    }
}
