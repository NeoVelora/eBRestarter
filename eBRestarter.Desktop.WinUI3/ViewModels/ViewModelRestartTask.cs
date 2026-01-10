using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelRestartTask : ObservableObject, IRecipient<UsernameChangedMessage>, IRecipient<BrowserChangedMessage>
    {
        private readonly IEVisitorConfigService _configService;
        private readonly IBrowserFactory _browserFactory;
        private readonly DispatcherQueue _dispatcherQueue;

        private DispatcherTimer? _uiTimer;
        private RestartTaskState _currentState = RestartTaskState.Idle;
        private IBrowser? _currentBrowser;

        private int _runtimeSeconds = 3600;
        private int _pauseSeconds = 20;
        private const int _initialDelaySeconds = 5;
        private const string _baseUrl = "https://www.ebesucher.com/surfbar/";

        [ObservableProperty] public partial int SecondsRemaining { get; set; }
        [ObservableProperty] public partial string Username { get; set; } = "-";
        [ObservableProperty] public partial string ChoosenBrowser { get; set; } = "Nicht gewählt";
        [ObservableProperty] public partial bool IsActive { get; set; } = false;
        [ObservableProperty] public partial string StatusInfoText { get; set; } = "Bereit.";

        public ViewModelRestartTask(IEVisitorConfigService configService, IBrowserFactory browserFactory)
        {
            _configService = configService;
            _browserFactory = browserFactory;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            LoadInitialData();
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private void LoadInitialData()
        {
            var config = _configService.LoadConfig();
            Username = config.Username ?? "-";
            ChoosenBrowser = config.Browser?.Selected ?? "Nicht gewählt";

            _runtimeSeconds = config.Browser.RuntimeHours * 3600;
            _pauseSeconds = config.Browser.RuntimePauseSeconds;
        }

        public void Receive(UsernameChangedMessage message) =>
            _dispatcherQueue.TryEnqueue(() => Username = message.NewUsername ?? "-");

        public void Receive(BrowserChangedMessage message) =>
            _dispatcherQueue.TryEnqueue(() => ChoosenBrowser = message.BrowserName ?? "-");

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


        private void StartLoop()
        {
            if (_uiTimer == null)
            {
                _uiTimer = new DispatcherTimer();
                _uiTimer.Interval = TimeSpan.FromSeconds(1);
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

        private void HandleStateTransition()
        {
            switch (_currentState)
            {
                case RestartTaskState.InitialDelay:
                    SwitchState(RestartTaskState.Running);
                    break;

                case RestartTaskState.Running:
                    SwitchState(RestartTaskState.Cooldown);
                    break;

                case RestartTaskState.Cooldown:
                    SwitchState(RestartTaskState.Running);
                    break;
            }
        }

        private void SwitchState(RestartTaskState newState)
        {
            _currentState = newState;

            switch (_currentState)
            {
                case RestartTaskState.Idle:
                    StatusInfoText = "Restarter gestoppt.";
                    SecondsRemaining = 0;
                    break;

                case RestartTaskState.InitialDelay:
                    SecondsRemaining = _initialDelaySeconds;
                    StatusInfoText = $"Start in {SecondsRemaining}s...";
                    break;

                case RestartTaskState.Running:
                    LaunchBrowser();
                    SecondsRemaining = _runtimeSeconds;
                    StatusInfoText = "Browser läuft.";
                    break;

                case RestartTaskState.Cooldown:
                    CloseCurrentBrowser();


                    SecondsRemaining = _pauseSeconds;
                    StatusInfoText = $"Neustart in {SecondsRemaining}s...";
                    break;
            }
        }

        private void UpdateDynamicStatusText()
        {
            if (_currentState == RestartTaskState.InitialDelay)
                StatusInfoText = $"Start in {SecondsRemaining}s...";
            else if (_currentState == RestartTaskState.Cooldown)
                StatusInfoText = $"Neustart in {SecondsRemaining}s...";
        }


        private void LaunchBrowser()
        {
            try
            {
                var browserType = GetBrowserTypeFromString(ChoosenBrowser);

                _currentBrowser = _browserFactory.Create(browserType);

                string url = $"{_baseUrl}{Username}";

                _currentBrowser.Start(url);
            }
            catch (Exception ex)
            {
                StatusInfoText = $"Fehler: {ex.Message}";
                IsActive = false;
            }
        }

        private void CloseCurrentBrowser()
        {
            _currentBrowser?.Close();
            _currentBrowser = null;
        }

        private BrowserType GetBrowserTypeFromString(string browserName)
        {
            if (string.IsNullOrWhiteSpace(browserName) || browserName == "Nicht gewählt")
                return BrowserType.Chrome;

            if (Enum.TryParse(browserName, true, out BrowserType type))
            {
                return type;
            }

            return BrowserType.Chrome;
        }
    }
}