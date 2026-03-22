using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.UseCases.ManageRestarterCycle;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelRestartTask : ObservableObject,
                                                IRecipient<UsernameChangedMessage>,
                                                IRecipient<BrowserChangedMessage>,
                                                IRecipient<DeleteBrowserContentActivateMessage>,
                                                IRecipient<DeleteBrowserContentIsActive>,
                                                IRecipient<NextDeletionProcess>,
                                                IRecipient<NextDeletionProcessDate>
    {
        #region FieldsAndInjectedServices

        private readonly IManageRestarterCycleUseCase _manageRestarterCycleUseCase;
        private readonly IEVisitorConfigService _configService;
        private readonly IDialogService _dialogService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ILocalizationService _localizationService;
        private readonly IRestartTaskDisplayStateService _restartTaskDisplayStateService;

        private int _pauseSeconds = 20;
        private int _runtimeSeconds = 3600;
        private readonly int _testRuntimeSeconds = 20;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial string ChosenBrowser { get; set; }
        [ObservableProperty] public partial bool DeleteBrowserContentIsActive { get; set; } = false;
        [ObservableProperty] public partial string DeleteIsActivatedMessage { get; set; } = "Disable";
        [ObservableProperty] public partial bool IsActive { get; set; } = false;
        [ObservableProperty] public partial string? NextDeletionProcessDateMessage { get; set; }
        [ObservableProperty] public partial string? NextDeletionProcessMessage { get; set; }
        [ObservableProperty] public partial int SecondsRemaining { get; set; }
        [ObservableProperty] public partial string StatusInfoText { get; set; }
        [ObservableProperty] public partial string Username { get; set; } = "-";

        #endregion

        #region ConstructorAndFinalizer

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public ViewModelRestartTask(
            IManageRestarterCycleUseCase manageRestarterCycleUseCase,
            IEVisitorConfigService configService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IRestartTaskDisplayStateService restartTaskDisplayStateService)
        {
            _manageRestarterCycleUseCase = manageRestarterCycleUseCase;
            _configService = configService;
            _dialogService = dialogService;
            _localizationService = localizationService;
            _restartTaskDisplayStateService = restartTaskDisplayStateService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            ChosenBrowser = _localizationService.GetString("Task_DefaultBrowser");
            StatusInfoText = _localizationService.GetString("Task_StatusReady");

            _manageRestarterCycleUseCase.ProgressChanged += OnCycleProgressChanged;

            LoadInitialConfigData();

            WeakReferenceMessenger.Default.RegisterAll(this);

            var appConfig = _configService.LoadConfig();
            if (appConfig.Browser?.StartBrowserWithProgrammStart == true)
            {
                IsActive = true;
                StartLoop();
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void ExecuteStartTimerScheduler(bool? isChecked)
        {
            var appConfig = _configService.LoadConfig();

            if (string.IsNullOrEmpty(appConfig.Username))
            {
                _localizationService.GetString("Task_DefaultBrowser");

                _dialogService.ShowMessageAsync(_localizationService.GetString("Task_Username"), _localizationService.GetString("Task_NoUsernameFound"), DialogIcon.Error);

                IsActive = false;

                return;

            }

            IsActive = isChecked ?? false;

            if (IsActive)
            {
                LoadInitialConfigData();

                StartLoop();
            }
            else
            {
                StopLoop();
            }
        }

        #endregion

        #region PublicAndProtectedMethods

        public void Receive(BrowserChangedMessage message) => _dispatcherQueue.TryEnqueue(() => ChosenBrowser = message.BrowserName ?? "-");
        public void Receive(UsernameChangedMessage message) => _dispatcherQueue.TryEnqueue(() => Username = message.NewUsername ?? "-");
        public void Receive(DeleteBrowserContentActivateMessage message) => _dispatcherQueue.TryEnqueue(() => DeleteIsActivatedMessage = message.ActivateMessage ?? "-");
        public void Receive(DeleteBrowserContentIsActive message) => _dispatcherQueue.TryEnqueue(() => DeleteBrowserContentIsActive = message.IsActiveOrNot);
        public void Receive(NextDeletionProcess message) => _dispatcherQueue.TryEnqueue(() => NextDeletionProcessMessage = message.NextDeletionProcessMessage ?? "-");
        public void Receive(NextDeletionProcessDate message) => _dispatcherQueue.TryEnqueue(() => NextDeletionProcessDateMessage = message.NextDeletionProcessDateMessage ?? "-");

        #endregion

        #region PrivateHelperMethods

        private void OnCycleProgressChanged(object? sender, RestarterCycleProgress e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                SecondsRemaining = e.SecondsRemaining;
                StatusInfoText = e.StatusMessage;

                if (e.State == RestartTaskState.Idle && IsActive)
                {
                    IsActive = false;
                }
            });
        }

        private void StartLoop()
        {
            var request = new ManageRestarterCycleRequest(
                BrowserDisplayName: ChosenBrowser,
                Username: Username,
                RuntimeSeconds: _runtimeSeconds,
                PauseSeconds: _pauseSeconds
            );

            _manageRestarterCycleUseCase.StartAsync(request, async () =>
            {
                var tcs = new TaskCompletionSource();

                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await _dialogService.ShowDeleteBrowserContentDialogAsync(autoStart: true);
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        LoadInitialConfigData();
                    }
                });

                await tcs.Task;

            }).Forget();
        }

        private void StopLoop()
        {
            _manageRestarterCycleUseCase.Stop();
        }

        private void LoadInitialConfigData()
        {
            var appConfig = _configService.LoadConfig();

            var currentConfigState = _restartTaskDisplayStateService.GetInitialState(appConfig);

            Username = currentConfigState.Username;
            ChosenBrowser = currentConfigState.ChoosenBrowser;

            _pauseSeconds = currentConfigState.PauseSeconds;

#if DEBUG
            _runtimeSeconds = _testRuntimeSeconds;
#else
            _runtimeSeconds = currentConfigState.RuntimeSeconds;
#endif

            DeleteBrowserContentIsActive = currentConfigState.DeleteBrowserContentIsActive;
            DeleteIsActivatedMessage = currentConfigState.DeleteIsActivatedMessage;
            NextDeletionProcessMessage = currentConfigState.NextDeletionProcessMessage;
            NextDeletionProcessDateMessage = currentConfigState.NextDeletionProcessDateMessage;
        }

        #endregion
    }
}