using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Domain.Entities;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using eBRestarter.Core.Application.Models;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelDeleteBrowserContent : ObservableObject
{
    private const int AutoCloseAfterSuccessDelayMilliseconds = 1000;

    private const int AutoSequenceStartupDelayMilliseconds = 500;

    private const string InitialProgressTextDisplay = "0 %";

    private bool _isAutoMode;

    private readonly IOutboundPortBrowserFactory _browserFactory;

    private readonly IUseCaseDeleteBrowserContent _deleteBrowserContentUseCase;

    private readonly IOutboundPortEVisitorConfigRepository _EVRestarterConfigRepository;

    private readonly IInboundPortLocalizationProvider _localizationService;

    private BrowserType _selectedBrowserType;

    private CancellationTokenSource? _deleteBrowserContentCancellationTokenSource;


    [ObservableProperty]
    public partial string BrowserIconPath { get; set; }

    [ObservableProperty]
    public partial string BrowserName { get; set; }

    [ObservableProperty]
    public partial string ProgressText { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; }


    [ObservableProperty]
    public partial double ProgressMaximum { get; set; }

    [ObservableProperty]
    public partial double ProgressValue { get; set; }


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCleaningCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
    public partial bool IsDeleteCookiesChecked { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
    public partial bool IsDeleteInternetCacheChecked { get; set; }

    [ObservableProperty]
    public partial bool IsProcessConflict { get; set; }

    public event Action? RequestCloseDialog;

    public ViewModelDeleteBrowserContent(
        IUseCaseDeleteBrowserContent deleteBrowserContentUseCase,
        IOutboundPortBrowserFactory BrowserFactory,
        IOutboundPortEVisitorConfigRepository EVRestarterConfigRepository,
        IInboundPortLocalizationProvider LocalizationProvider)
    {
        ArgumentNullException.ThrowIfNull(deleteBrowserContentUseCase);
        ArgumentNullException.ThrowIfNull(BrowserFactory);
        ArgumentNullException.ThrowIfNull(EVRestarterConfigRepository);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);

        _deleteBrowserContentUseCase = deleteBrowserContentUseCase;
        _browserFactory = BrowserFactory;
        _EVRestarterConfigRepository = EVRestarterConfigRepository;
        _localizationService = LocalizationProvider;

        BrowserIconPath = string.Empty;
        BrowserName = _localizationService.RetrieveString("Cleanup_Loading");
        ProgressText = InitialProgressTextDisplay;
        StatusText = _localizationService.RetrieveString("Cleanup_Ready");
        ProgressMaximum = 100;
        ProgressValue = 0;
        IsDeleteCookiesChecked = true;
        IsDeleteInternetCacheChecked = true;

        AppConfig appConfig = _EVRestarterConfigRepository.LoadConfig();
        string selectedBrowserString = appConfig.Browser.Selected;

        if (Enum.TryParse<BrowserType>(selectedBrowserString, ignoreCase: true, out BrowserType parsedBrowserType))
        {
            Initialize(parsedBrowserType);
        }
        else
        {
            string errorFormat = _localizationService.RetrieveString("Cleanup_ConfigError");
            StatusText = string.Format(errorFormat, selectedBrowserString);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelCleaning()
    {
        _deleteBrowserContentCancellationTokenSource?.Cancel();
        StatusText = _localizationService.RetrieveString("Cleanup_Canceling");
    }

    [RelayCommand]
    private void CancelConflict()
    {
        IsProcessConflict = false;
        StatusText = _localizationService.RetrieveString("Cleanup_CanceledByUser");
    }

    [RelayCommand]
    private async Task ForceCloseAndContinue()
    {
        IsProcessConflict = false;
        await ExecuteCleaningLogic(true);
    }

    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task StartCleaning()
    {
        if (IsBusy) return;
        await ExecuteCleaningLogic(false);
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
        catch (NotSupportedException ex)
        {
            string errorFormat = _localizationService.RetrieveString("General_LoadErrorPrefix");
            StatusText = string.Format(errorFormat, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            string errorFormat = _localizationService.RetrieveString("General_LoadErrorPrefix");
            StatusText = string.Format(errorFormat, ex.Message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            string errorFormat = _localizationService.RetrieveString("General_LoadErrorPrefix");
            StatusText = string.Format(errorFormat, ex.Message);
        }
    }

    public async Task RunAutoSequenceAsync()
    {
        _isAutoMode = true;
        await Task.Delay(AutoSequenceStartupDelayMilliseconds);
        await StartCleaning();
    }

    private bool CanCancel() => IsBusy;

    private bool CanClean() => !IsBusy && (IsDeleteCookiesChecked || IsDeleteInternetCacheChecked);

    private async Task ExecuteCleaningLogic(bool forceClose)
    {
        IsBusy = true;
        _deleteBrowserContentCancellationTokenSource?.Dispose();
        _deleteBrowserContentCancellationTokenSource = new CancellationTokenSource();

        try
        {
            var request = new DeleteBrowserContentRequest(
                BrowserType: _selectedBrowserType,
                DeleteCookies: IsDeleteCookiesChecked,
                DeleteCache: IsDeleteInternetCacheChecked,
                ForceCloseProcess: forceClose
            );

            var progress = new Progress<DeleteBrowserContentProgress>(cleanupProgress =>
            {
                StatusText = cleanupProgress.StatusMessage;
                ProgressMaximum = cleanupProgress.TotalFiles > 0 ? cleanupProgress.TotalFiles : 1;
                ProgressValue = cleanupProgress.CurrentFile;
                if (cleanupProgress.TotalFiles > 0)
                    ProgressText = $"{cleanupProgress.CurrentFile * 100 / cleanupProgress.TotalFiles} %";
            });

            var result = await _deleteBrowserContentUseCase.ExecuteAsync(
                request,
                progress,
                _deleteBrowserContentCancellationTokenSource.Token);

            if (result.HasError<ProcessConflictError>())
            {
                IsProcessConflict = true;
            }
            else if (result.IsFailed)
            {
                StatusText = result.Errors[0].Message;
            }
            else if (_isAutoMode)
            {
                await Task.Delay(AutoCloseAfterSuccessDelayMilliseconds);
                RequestCloseDialog?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = _localizationService.RetrieveString("Cleanup_CanceledByUser");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            string errorFormat = _localizationService.RetrieveString("General_ErrorPrefix");
            StatusText = string.Format(errorFormat, ex.Message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            string errorFormat = _localizationService.RetrieveString("General_ErrorPrefix");
            StatusText = string.Format(errorFormat, ex.Message);
        }
        finally
        {
            IsBusy = false;
            _deleteBrowserContentCancellationTokenSource?.Dispose();
            _deleteBrowserContentCancellationTokenSource = null;
        }
    }
}


