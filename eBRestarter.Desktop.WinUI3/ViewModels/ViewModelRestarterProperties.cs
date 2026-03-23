using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.UseCases.ScheduleBrowserCleanup;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Core.Domain.Models.Records.Config;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelRestarterProperties : ObservableObject
    {
        #region FieldsAndInjectedServices

        private readonly IScheduleBrowserCleanupUseCase _scheduleBrowserCleanupUseCase;
        private readonly AppConfig _currentConfig;
        private readonly IDialogService _dialogService;
        private readonly IEVisitorConfigService _eVisitorConfigService;
        private readonly ILocalizationService _localizationService;
        private readonly IOperatingSystemFacade _operatingSystemFacade;

        private readonly IBrowserService _browserService;
        private readonly DispatcherQueue? _dispatcherQueue;
        private readonly DispatcherTimer _browserCheckTimer;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial bool CheckBrowserIsAliveIsOn { get; set; } = false;
        [ObservableProperty] public partial int RuntimeHours { get; set; }
        [ObservableProperty] public partial int RuntimePauseSeconds { get; set; }
        [ObservableProperty] public partial BrowserCacheDeleteOption SelectedDeleteBrowserCacheOption { get; set; }
        [ObservableProperty] public partial bool StartBrowserWithProgrammStartIs { get; set; } = false;
        [ObservableProperty] private partial string StandardBrowser { get; set; } = string.Empty;
        [ObservableProperty] public partial Visibility Tbl_NoBrowserInstalledIsVisible { get; set; } = Visibility.Collapsed;
        [ObservableProperty] public partial bool BtnDeleteBrowserContentIsEnabled { get; set; } = true;
        [ObservableProperty] public partial bool BtnInstalleBesucherAddOnIsEnabled { get; set; } = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddEVisitorUsernameCommand))]
        public partial string Username { get; set; } = string.Empty;

        #endregion

        #region PublicProperties

        public ReadOnlyCollection<BrowserCacheDeleteOption> BrowserDeleteCacheOptionList { get; }
        public int BrowserRuntimeHoursMax { get; init; }
        public int BrowserRuntimeHoursMin { get; init; }
        public int RuntimePauseSecondsMax { get; init; }
        public int RuntimePauseSecondsMin { get; init; }

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelRestarterProperties(
            IScheduleBrowserCleanupUseCase scheduleBrowserCleanupUseCase,
            IOperatingSystemFacade operatingSystemFacade,
            IEVisitorConfigService eVisitorConfigService,
            ILocalizationService localizationService,
            IDialogService dialogService,
            IBrowserService browserService)
        {
            _scheduleBrowserCleanupUseCase = scheduleBrowserCleanupUseCase;
            _operatingSystemFacade = operatingSystemFacade;
            _eVisitorConfigService = eVisitorConfigService;
            _dialogService = dialogService;
            _localizationService = localizationService;
            _browserService = browserService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _currentConfig = _eVisitorConfigService.LoadConfig();

            RuntimePauseSecondsMin = 20;
            RuntimePauseSecondsMax = 60;
            BrowserRuntimeHoursMin = 1;
            BrowserRuntimeHoursMax = 12;

            RuntimePauseSeconds = _currentConfig.Browser.RuntimePauseSeconds;
            RuntimeHours = _currentConfig.Browser.RuntimeHours;
            StartBrowserWithProgrammStartIs = _currentConfig.Browser.StartBrowserWithProgrammStart;
            CheckBrowserIsAliveIsOn = _currentConfig.Browser.CheckBrowserAliveRoutine;

            BrowserDeleteCacheOptionList = new ReadOnlyCollection<BrowserCacheDeleteOption>([.. localizationService.GetBrowserCacheOptions()]);

            var configDays = _currentConfig.Browser.DeleteBrowserCacheIntervalDays;

            SelectedDeleteBrowserCacheOption = BrowserDeleteCacheOptionList.FirstOrDefault(option => option.Days == configDays) ?? BrowserDeleteCacheOptionList[0];

            _browserCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _browserCheckTimer.Tick += async (__, _) => await CheckInstalledBrowsersAsync();
            _browserCheckTimer.Start();

            CheckInstalledBrowsersAsync().Forget();
        }

        #endregion

        private async Task CheckInstalledBrowsersAsync()
        {
            var browsers = await Task.Run(() => _browserService.GetInstalledBrowsersAsync());

            bool hasInstalledBrowsers = browsers != null && browsers.Any(b => b.IsInstalled);

            _dispatcherQueue!.TryEnqueue(() =>
            {
                var newVisibility = hasInstalledBrowsers ? Visibility.Collapsed : Visibility.Visible;
                if (Tbl_NoBrowserInstalledIsVisible != newVisibility)
                {
                    Tbl_NoBrowserInstalledIsVisible = newVisibility;
                    BtnDeleteBrowserContentIsEnabled = hasInstalledBrowsers;
                    BtnInstalleBesucherAddOnIsEnabled = hasInstalledBrowsers;
                }
            });
        }

        #region Commands

        [RelayCommand(CanExecute = nameof(CanAddUsername))]
        private void AddEVisitorUsername()
        {
            _currentConfig.Username = Username;

            SaveSettings();

            WeakReferenceMessenger.Default.Send(new UsernameChangedMessage(Username));

            Username = string.Empty;
        }

        [RelayCommand]
        public void RegisterToEVisitor()
        {
            _operatingSystemFacade.WindowsProcessControlService.OpenUrlInBrowser(WebLinks.RegistrationLink, string.Empty);
        }

        [RelayCommand]
        private async Task ShowBrowserDeleteContent()
        {
            await _dialogService.ShowDeleteBrowserContentDialogAsync(autoStart: false);
        }

        [RelayCommand]
        private async Task ShowInstallAddOnDialog()
        {
            await _dialogService.ShowInstallAddOnDialogAsync();
        }

        [RelayCommand]
        private async Task ShowInstallAddOnInfoDialog()
        {
            await _dialogService.ShowInstallAddOnInfoDialogAsync();
        }

        [RelayCommand]
        private async Task OpenStartupBoostDialog()
        {
            await _dialogService.ShowTurnOffEdgeStartupBoostDialogAsync();
        }

        #endregion

        #region PublicAndProtectedMethods

        #endregion

        #region PropertyChangeHandlers

        partial void OnCheckBrowserIsAliveIsOnChanged(bool value)
        {
            _currentConfig.Browser.CheckBrowserAliveRoutine = value;

            SaveSettings();
        }

        partial void OnRuntimeHoursChanged(int value)
        {
            int clampedValue = Math.Clamp(value, BrowserRuntimeHoursMin, BrowserRuntimeHoursMax);

            if (value != clampedValue)
            {
                RuntimeHours = clampedValue;
                return;
            }

            if (_currentConfig.Browser.RuntimeHours != value)
            {
                _currentConfig.Browser.RuntimeHours = value;
                SaveSettings();
            }

        }

        partial void OnRuntimePauseSecondsChanged(int value)
        {
            int clampedValue = Math.Clamp(value, RuntimePauseSecondsMin, RuntimePauseSecondsMax);

            if (value != clampedValue)
            {
                RuntimePauseSeconds = clampedValue;
                return;
            }

            if (_currentConfig.Browser.RuntimePauseSeconds != value)
            {
                _currentConfig.Browser.RuntimePauseSeconds = value;
                SaveSettings();
            }
        }

        partial void OnSelectedDeleteBrowserCacheOptionChanged(BrowserCacheDeleteOption value)
        {
            var response = _scheduleBrowserCleanupUseCase.UpdateSchedule(new ScheduleBrowserCleanupRequest(value.Days));

            _currentConfig.Browser.DeleteBrowserCacheIntervalDays = value.Days;
            _currentConfig.Browser.NextBrowserDeleteCacheDate = response.NextDate ?? DateTime.MinValue;

            if (response.IsActive && response.NextDate.HasValue)
            {
                string formatPattern = _localizationService.GetString("Browser_NextDeleteDate_Format");
                string formattedDateString = string.Format(formatPattern, response.NextDate.Value);

                WeakReferenceMessenger.Default.Send(new NextDeletionProcess(_localizationService.GetString("NextDeletionProcess")));
                WeakReferenceMessenger.Default.Send(new NextDeletionProcessDate(formattedDateString));
                WeakReferenceMessenger.Default.Send(new DeleteBrowserContentActivateMessage(_localizationService.GetString("Activate")));
                WeakReferenceMessenger.Default.Send(new DeleteBrowserContentIsActive(true));
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new NextDeletionProcess(string.Empty));
                WeakReferenceMessenger.Default.Send(new NextDeletionProcessDate(string.Empty));
                WeakReferenceMessenger.Default.Send(new DeleteBrowserContentActivateMessage(_localizationService.GetString("Disabled")));
                WeakReferenceMessenger.Default.Send(new DeleteBrowserContentIsActive(false));
            }
        }

        partial void OnStartBrowserWithProgrammStartIsChanged(bool value)
        {
            _currentConfig.Browser.StartBrowserWithProgrammStart = value;
            SaveSettings();
        }

        #endregion

        #region PrivateHelperMethods

        private bool CanAddUsername() => !string.IsNullOrWhiteSpace(Username);

        private void SaveSettings()
        {
            _eVisitorConfigService.SaveConfig(_currentConfig);
        }

        #endregion
    }
}
