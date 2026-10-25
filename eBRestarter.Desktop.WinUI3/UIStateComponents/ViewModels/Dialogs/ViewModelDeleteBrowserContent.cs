using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Common.Attributes;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Dialogs;

public sealed partial class ViewModelDeleteBrowserContent : ObservableObject
{
    private const int AutoCloseAfterSuccessDelayMilliseconds = 1000;
    private const int AutoSequenceStartupDelayMilliseconds = 500;
    private const int DefaultProgressMaximumValue = 100;
    private const string InitialProgressTextDisplay = "0 %";
    private const string KeyCleanupCanceledByUser = "Cleanup_CanceledByUser";
    private const string KeyCleanupCanceling = "Cleanup_Canceling";
    private const string KeyCleanupConfigError = "Cleanup_ConfigError";
    private const string KeyCleanupFailed = "Cleanup_Failed";
    private const string KeyCleanupFileError = "Cleanup_FileError";
    private const string KeyCleanupLoading = "Cleanup_Loading";
    private const string KeyCleanupReady = "Cleanup_Ready";
    private const string KeyGeneralLoadError = "General_LoadError";

    private readonly IOutboundPortBrowserFactory _browserFactory;
    private readonly ILogger<ViewModelDeleteBrowserContent> _logger;
    private readonly DeleteBrowserContentUseCase _deleteBrowserContentUseCase;
    private readonly IOutboundPortEVisitorConfigRepository _evRestarterConfigRepository;
    private readonly IInboundPortLocalizationProvider _localizationService;

    private bool _isAutoMode;

    private BrowserType _selectedBrowserType;

    private CancellationTokenSource? _deleteBrowserContentCancellationTokenSource;


    public ViewModelDeleteBrowserContent(
        DeleteBrowserContentUseCase deleteBrowserContentUseCase,
        IOutboundPortBrowserFactory browserFactory,
        IOutboundPortEVisitorConfigRepository evRestarterConfigRepository,
        IInboundPortLocalizationProvider localizationService,
        ILogger<ViewModelDeleteBrowserContent> logger)
    {
        ArgumentNullException.ThrowIfNull(deleteBrowserContentUseCase);
        ArgumentNullException.ThrowIfNull(browserFactory);
        ArgumentNullException.ThrowIfNull(evRestarterConfigRepository);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _deleteBrowserContentUseCase = deleteBrowserContentUseCase;
        _browserFactory = browserFactory;
        _evRestarterConfigRepository = evRestarterConfigRepository;
        _localizationService = localizationService;

        BrowserIconPath = string.Empty;
        BrowserName = _localizationService.RetrieveString(KeyCleanupLoading);
        ProgressText = InitialProgressTextDisplay;
        StatusText = _localizationService.RetrieveString(KeyCleanupReady);
        ProgressMaximum = DefaultProgressMaximumValue;
        ProgressValue = 0;
        IsDeleteCookiesChecked = true;
        IsDeleteInternetCacheChecked = true;

        AppConfig appConfig = _evRestarterConfigRepository.LoadConfig();
        string selectedBrowserString = appConfig?.Browser?.Selected ?? string.Empty;

        if (Enum.TryParse<BrowserType>(selectedBrowserString, ignoreCase: true, out BrowserType parsedBrowserType))
        {
            Initialize(parsedBrowserType);
        }
        else
        {
            string errorFormat = _localizationService.RetrieveString(KeyCleanupConfigError);
            StatusText = string.Format(CultureInfo.CurrentCulture, errorFormat, selectedBrowserString);
        }
    }


    public event Action? RequestCloseDialog;


    [ObservableProperty]
    public partial string BrowserIconPath { get; set; }

    [ObservableProperty]
    public partial string BrowserName { get; set; }

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

    [ObservableProperty]
    public partial double ProgressMaximum { get; set; }

    [ObservableProperty]
    public partial string ProgressText { get; set; }

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; }


    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelCleaning()
    {
        _deleteBrowserContentCancellationTokenSource?.Cancel();
        StatusText = _localizationService.RetrieveString(KeyCleanupCanceling);
    }

    [RelayCommand]
    private void CancelConflict()
    {
        IsProcessConflict = false;
        StatusText = _localizationService.RetrieveString(KeyCleanupCanceledByUser);
    }

    private bool CanCancel() => IsBusy;

    private bool CanClean() => !IsBusy && (IsDeleteCookiesChecked || IsDeleteInternetCacheChecked);

    [ExceptionBoundary(ExceptionBoundaryKind.ThreadRoot, Justification = "UI command boundary for browser cache cleanup execution.")]
    private async Task ExecuteCleaningLogicAsync(bool forceClose)
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

                ProgressMaximum = cleanupProgress.TotalSteps > 0 ? cleanupProgress.TotalSteps : 1;
                ProgressValue = cleanupProgress.CompletedSteps;

                if (cleanupProgress.TotalSteps > 0)
                {
                    ProgressText = $"{cleanupProgress.CompletedSteps * 100 / cleanupProgress.TotalSteps} %";
                }
            });

            var outcome = await _deleteBrowserContentUseCase.ExecuteAsync(
                request,
                progress,
                _deleteBrowserContentCancellationTokenSource.Token);

            switch (outcome)
            {
                case DeleteBrowserContentProcessConflict:
                    IsProcessConflict = true;
                    break;

                case DeleteBrowserContentFailure failure:
                    StatusText = _localizationService.RetrieveString(failure.Message);
                    break;

                case DeleteBrowserContentSuccess:
                    if (_isAutoMode)
                    {
                        await Task.Delay(AutoCloseAfterSuccessDelayMilliseconds);
                        RequestCloseDialog?.Invoke();
                    }
                    break;
            }
        }
        catch (OperationCanceledException) when (_deleteBrowserContentCancellationTokenSource?.IsCancellationRequested == true)
        {
            StatusText = _localizationService.RetrieveString(KeyCleanupCanceledByUser);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(
                LogEventIds.Browser.BrowserCacheFileDeletionFailed,
                ex,
                "Browser cleanup failed with a file system error.");

            StatusText = _localizationService.RetrieveString(KeyCleanupFileError);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                LogEventIds.Browser.BrowserCacheFileDeletionFailed,
                ex,
                "Browser cleanup failed unexpectedly.");

            StatusText = _localizationService.RetrieveString(KeyCleanupFailed);
        }
        finally
        {
            IsBusy = false;
            _deleteBrowserContentCancellationTokenSource?.Dispose();
            _deleteBrowserContentCancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private async Task ForceCloseAndContinueAsync()
    {
        IsProcessConflict = false;
        await ExecuteCleaningLogicAsync(true);
    }

    [ExceptionBoundary(ExceptionBoundaryKind.ThreadRoot, Justification = "UI initialization boundary for browser cleanup dialog state.")]
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
            _logger.LogError(
                LogEventIds.UserInterface.ViewModelOperationFailed,
                ex,
                "Loading the browser cleanup dialog state failed.");

            StatusText = _localizationService.RetrieveString(KeyGeneralLoadError);
        }
    }

    public async Task RunAutoSequenceAsync()
    {
        _isAutoMode = true;
        await Task.Delay(AutoSequenceStartupDelayMilliseconds);
        await StartCleaningAsync();
    }

    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task StartCleaningAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await ExecuteCleaningLogicAsync(false);
    }
}
