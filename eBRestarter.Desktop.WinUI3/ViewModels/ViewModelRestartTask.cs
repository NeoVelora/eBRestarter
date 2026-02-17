using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
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
        #region ConstantsAndStatics

        private const string BaseUrl = "https://www.ebesucher.com/surfbar/";
        private const int InitialDelaySeconds = 5;

        #endregion

        #region FieldsAndInjectedServices

        private readonly IBrowserCleanupScheduleService _browserCleanupScheduleService;
        private readonly IBrowserDisplayNameResolver _browserDisplayNameResolver;
        private readonly IBrowserFactory _browserFactory;
        private readonly IEVisitorConfigService _configService;
        private readonly IDialogService _dialogService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ILocalizationService _localizationService;
        private readonly IRestartTaskDisplayStateService _restartTaskDisplayStateService;
        private IBrowser? _currentBrowser;
        private RestartTaskState _currentState = RestartTaskState.Idle;
        private bool _isTestMode = false;
        private int _pauseSeconds = 20;
        private int _runtimeSeconds = 3600;
        private int _testRuntimeSeconds = 20;
        private DispatcherTimer? _uiTimer;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial string ChosenBrowser { get; set; }
        [ObservableProperty] public partial bool DeleteBrowserContentIsActive { get; set; } = false;
        [ObservableProperty] public partial string DeleteIsActivatedMessage { get; set; } = "Disable";
        [ObservableProperty] public partial bool IsActive { get; set; } = false;
        [ObservableProperty] public partial string NextDeletionProcessDateMessage { get; set; }
        [ObservableProperty] public partial string NextDeletionProcessMessage { get; set; }
        [ObservableProperty] public partial int SecondsRemaining { get; set; }
        [ObservableProperty] public partial string StatusInfoText { get; set; }
        [ObservableProperty] public partial string Username { get; set; } = "-";

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelRestartTask(
            IEVisitorConfigService configService,
            IBrowserFactory browserFactory,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IRestartTaskDisplayStateService restartTaskDisplayStateService,
            IBrowserCleanupScheduleService browserCleanupScheduleService,
            IBrowserDisplayNameResolver browserDisplayNameResolver)
        {
            _configService = configService;
            _browserFactory = browserFactory;
            _dialogService = dialogService;
            _localizationService = localizationService;
            _restartTaskDisplayStateService = restartTaskDisplayStateService;
            _browserCleanupScheduleService = browserCleanupScheduleService;
            _browserDisplayNameResolver = browserDisplayNameResolver;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            ChosenBrowser = _localizationService.GetString("Task_DefaultBrowser");
            StatusInfoText = _localizationService.GetString("Task_StatusReady");

            LoadInitialData();
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void ExecuteStartTimerScheduler(bool? isChecked)
        {
            IsActive = isChecked ?? false;

            if (IsActive)
            {
                LoadInitialData();
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

        private void CloseCurrentBrowser()
        {
            _currentBrowser?.Close();
            _currentBrowser = null;
        }

        private BrowserType GetBrowserTypeFromString(string browserName)
        {
            string defaultText = _localizationService.GetString("Task_DefaultBrowser");
            return _browserDisplayNameResolver.GetBrowserTypeFromDisplayName(browserName, defaultText);
        }

        private async void HandleStateTransition()
        {
            switch (_currentState)
            {
                case RestartTaskState.InitialDelay:
                    SwitchState(RestartTaskState.Running);
                    break;

                case RestartTaskState.Running:
                    await CheckAndExecuteBrowserCleanup();
                    SwitchState(RestartTaskState.Cooldown);
                    break;

                case RestartTaskState.Cooldown:
                    SwitchState(RestartTaskState.Running);
                    break;
            }
        }

        private async Task CheckAndExecuteBrowserCleanup()
        {
            var appConfig = _configService.LoadConfig();
            if (!_browserCleanupScheduleService.ShouldRunCleanupNow(appConfig))
                return;

            CloseCurrentBrowser();
            await Task.Delay(1000);

            await _dialogService.ShowDeleteBrowserContentDialogAsync(autoStart: true);

            var newDate = _browserCleanupScheduleService.GetNextCleanupDateAfterRun(DateTime.Today, appConfig.Browser.DeleteBrowserCacheIntervalDays);
            appConfig.Browser.NextBrowserDeleteCacheDate = newDate;
            _configService.SaveConfig(appConfig);

            LoadInitialData();
        }

        private void LaunchBrowser()
        {
            try
            {
                var browserType = GetBrowserTypeFromString(ChosenBrowser);
                _currentBrowser = _browserFactory.Create(browserType);
                string url = $"{BaseUrl}{Username}";
                _currentBrowser.Start(url);
            }
            catch (Exception ex)
            {
                string errorFormat = _localizationService.GetString("General_ErrorPrefix");
                StatusInfoText = string.Format(errorFormat, ex.Message);
                IsActive = false;
            }
        }

        private void LoadInitialData()
        {
            var appConfig = _configService.LoadConfig();
            var state = _restartTaskDisplayStateService.GetInitialState(appConfig);

            Username = state.Username;
            ChosenBrowser = state.ChoosenBrowser;

            if (_isTestMode)
            {
                _runtimeSeconds = _testRuntimeSeconds;
                _pauseSeconds = 5;
                StatusInfoText = $"[TEST] Runtime: {_runtimeSeconds}s";
            }
            else
            {
                _runtimeSeconds = state.RuntimeSeconds;
                _pauseSeconds = state.PauseSeconds;
            }

            DeleteBrowserContentIsActive = state.DeleteBrowserContentIsActive;
            DeleteIsActivatedMessage = state.DeleteIsActivatedMessage;
            NextDeletionProcessMessage = state.NextDeletionProcessMessage;
            NextDeletionProcessDateMessage = state.NextDeletionProcessDateMessage;
        }

        private void OnTimerTick(object? sender, object e)
        {
            if (SecondsRemaining > 0)
            {
                SecondsRemaining--;
                UpdateDynamicStatusText();
            }
            else
            {
                HandleStateTransition();
            }
        }

        private void StartLoop()
        {
            if (_uiTimer == null)
            {
                _uiTimer = new DispatcherTimer();
                _uiTimer.Interval = TimeSpan.FromMilliseconds(1000);
                _uiTimer.Tick += OnTimerTick;
            }

            SwitchState(RestartTaskState.InitialDelay);
            _uiTimer.Start();
        }

        private void StopLoop()
        {
            if (_uiTimer != null)
            {
                _uiTimer.Stop();
                _uiTimer.Tick -= OnTimerTick;
                _uiTimer = null;
            }

            CloseCurrentBrowser();
            SwitchState(RestartTaskState.Idle);
        }

        private void SwitchState(RestartTaskState newState)
        {
            _currentState = newState;

            switch (_currentState)
            {
                case RestartTaskState.Idle:
                    StatusInfoText = _localizationService.GetString("Task_StatusStopped");
                    SecondsRemaining = 0;
                    break;

                case RestartTaskState.InitialDelay:
                    SecondsRemaining = InitialDelaySeconds;
                    UpdateDynamicStatusText();
                    break;

                case RestartTaskState.Running:
                    LaunchBrowser();
                    SecondsRemaining = _runtimeSeconds;
                    StatusInfoText = _localizationService.GetString("Task_StatusRunning");
                    break;

                case RestartTaskState.Cooldown:
                    CloseCurrentBrowser();
                    SecondsRemaining = _pauseSeconds;
                    UpdateDynamicStatusText();
                    break;
            }
        }

        private void UpdateDynamicStatusText()
        {
            if (_currentState == RestartTaskState.InitialDelay)
            {
                string statusFormat = _localizationService.GetString("Task_StatusStartIn");
                StatusInfoText = string.Format(statusFormat, SecondsRemaining);
            }
            else if (_currentState == RestartTaskState.Cooldown)
            {
                string statusFormat = _localizationService.GetString("Task_StatusRestartIn");
                StatusInfoText = string.Format(statusFormat, SecondsRemaining);
            }
        }

        #endregion
    }
}
