using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Interfaces.Update;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Core.Domain.Models.Records.Config;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Infrastructure.Constants;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelOptions : ObservableObject
    {

        private bool _isInitializing = false;

        private readonly AppConfig _currentConfig;
        private readonly IEVisitorConfigService _eVisitorConfigService;
        private readonly IDialogService _dialogService;
        private readonly IWindowsAutoLogonService _autoLogonService;
        private readonly IOperatingSystemFacade _os;
        private readonly IUpdateService _updateService;
        private readonly IThemeService _themeService;
        private readonly ILanguageService _languageService;


        public ReadOnlyCollection<ComputerRestartOption> ComputerRestartList => ComputerRestartConstants.Options;
        public ReadOnlyCollection<LanguageOption> LanguageList => LanguageSelectionConstants.Options;

        public int ComputerRestartClockTimeMin { get; init; }
        public int ComputerRestartClockTimeMax { get; init; }

        [ObservableProperty] public partial ComputerRestartOption SelectedComputerRestartOption { get; set; }
        [ObservableProperty] public partial LanguageOption SelectedLanguageOption { get; set; }
        [ObservableProperty] public partial int ComputerRestartClockTime { get; set; }
        [ObservableProperty] public partial bool StartWithWindows { get; set; } = false;
        [ObservableProperty] public partial bool IsRestartSliderVisible { get; set; }
        [ObservableProperty] public partial string RestartStatusText { get; set; } = string.Empty;

        [ObservableProperty] public partial bool IsUpdateAvailable { get; set; }
        [ObservableProperty] public partial string UpdateMessage { get; set; } = string.Empty;


        public ViewModelOptions(
            IDialogService dialogService,
            IWindowsAutoLogonService autoLogonService,
            IOperatingSystemFacade os,
            IUpdateService updateService,
            IThemeService themeService,
            IEVisitorConfigService eVisitorConfigService,
            ILanguageService languageService)
        {
            _isInitializing = true;

            _dialogService = dialogService;
            _autoLogonService = autoLogonService;
            _os = os;
            _updateService = updateService;
            _themeService = themeService;
            _eVisitorConfigService = eVisitorConfigService;
            _languageService = languageService;

            _currentConfig = _eVisitorConfigService.LoadConfig();

            ComputerRestartClockTimeMin = 0;
            ComputerRestartClockTimeMax = 23;
            ComputerRestartClockTime = _currentConfig.Computer.RestartClockTime;

            var configDays = _currentConfig.Browser.DeleteBrowserCacheIntervalDays;
            var configLanguageIndex = _currentConfig.Settings.Language;

            SelectedComputerRestartOption = ComputerRestartList.FirstOrDefault(x => x.Days == configDays) ?? ComputerRestartList[0];

            SelectedLanguageOption = LanguageList.FirstOrDefault(x => x.Index == configLanguageIndex) ?? LanguageList[0];

            _isInitializing = false;

            _ = InitializeAsync();

        }


        [RelayCommand]
        private async Task CheckForUpdates()
        {
            try
            {
                var info = await _updateService.CheckForUpdateAsync();

                if (info.IsUpdateAvailable)
                {
                    IsUpdateAvailable = true;
                    UpdateMessage = $"Version {info.LatestVersion} verfügbar!";

                }
            }
            catch (Exception ex)
            {
            }
        }

        [RelayCommand]
        private async Task PerformUpdate()
        {
            var info = await _updateService.CheckForUpdateAsync();
            if (info.IsUpdateAvailable)
            {
                await _updateService.DownloadAndInstallAsync(info);
            }
        }

        [RelayCommand]
        private async Task OpenSettingsDataFolder()
        {
            _os.WindowsProcessControlService.OpenExplorer(SystemPaths.ApplicationDataBasePath);
        }

        [RelayCommand]
        private void SetLightTheme()
        {
            _themeService.SetTheme("Light");
            SaveThemeConfig("Light");
        }

        [RelayCommand]
        private void SetDarkTheme()
        {
            _themeService.SetTheme("Dark");
            SaveThemeConfig("Dark");
        }

        private void SaveThemeConfig(string theme)
        {
            var config = _eVisitorConfigService.LoadConfig();
            var newConfig = config with { Settings = config.Settings with { Theme = theme } };
            _eVisitorConfigService.SaveConfig(newConfig);
        }

        [RelayCommand]
        private async Task ShowActivateApiDialog()
        {
            await _dialogService.ShowActivateApiDialogAsync();
        }

        [RelayCommand]
        private async Task ShowImportApiDialog()
        {
            await _dialogService.ShowImportApiDialogAsync();
        }

        [RelayCommand]
        private async Task RemoveAPICredentials()
        {
            var currentConfig = _eVisitorConfigService.LoadConfig();
            var newConfig = currentConfig with
            {
                Settings = currentConfig.Settings with
                {
                    ApiUsername = string.Empty,
                    ApiKey = string.Empty
                }
            };
            _eVisitorConfigService.SaveConfig(newConfig);

            await _dialogService.ShowMessageAsync("API Zugang", "Deine API Zugangsdaten wurden entfernt.", DialogIcon.Success);
        }

        [RelayCommand]
        private async Task ConfigureAutoLogon()
        {
            string currentUser = Environment.UserName;
            string currentDomain = Environment.UserDomainName;

            var dialogResult = await _dialogService.ShowAutoLogonDialogAsync(currentUser, currentDomain);

            if (dialogResult == null) return;

            try
            {
                if (dialogResult.IsDeactivateAction)
                {
                    _autoLogonService.DisableAutoLogon();
                    await _dialogService.ShowMessageAsync("Info", "Die automatische Anmeldung wurde deaktiviert.");
                }
                else if (dialogResult.Credentials != null)
                {
                    var user = dialogResult.Credentials.Username;
                    var domain = dialogResult.Credentials.Domain;
                    var pass = dialogResult.Credentials.Password;

                    bool isValid = ValidateCredentials(user, domain, pass);

                    if (!isValid)
                    {
                        await _dialogService.ShowMessageAsync("Fehler", "Benutzername oder Passwort sind nicht korrekt. Bitte prüfen Sie die Eingaben.");
                        return;
                    }

                    _autoLogonService.EnableAutoLogon(user, domain, pass);
                    await _dialogService.ShowMessageAsync("Erfolg", "Die automatische Anmeldung wurde eingerichtet.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Fehler", $"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
            }
        }


        partial void OnSelectedComputerRestartOptionChanged(ComputerRestartOption value)
        {
            _currentConfig.Computer.ComputerRestartIntervalDays = value.Days;

            RecalculateNextRestartDate();
            UpdateRestartUiState();
            SaveSettings();
        }

        partial void OnComputerRestartClockTimeChanged(int value)
        {
            int clampedValue = Math.Clamp(value, ComputerRestartClockTimeMin, ComputerRestartClockTimeMax);
            if (value != clampedValue)
            {
                ComputerRestartClockTime = clampedValue;
                return;
            }

            if (_currentConfig.Computer.RestartClockTime != value)
            {
                _currentConfig.Computer.RestartClockTime = value;
                RecalculateNextRestartDate();
                UpdateRestartUiState();
                SaveSettings();
            }
        }

        async partial void OnSelectedLanguageOptionChanged(LanguageOption value)
        {
            if (value == null || _isInitializing) return;

            if (_currentConfig.Settings.Language != value.Index)
            {
                _currentConfig.Settings.Language = value.Index;
                SaveSettings();
            }

            string newLanguageCode = value.Index == 0 ? "de-DE" : "en-US";

            if (_languageService.CurrentLanguageCode != newLanguageCode)
            {
                _languageService.SetLanguage(newLanguageCode);

                bool restartNow = await _dialogService.ShowConfirmationAsync(
                    "Neustart erforderlich / Restart required",
                    "Die Sprache wurde geändert. Damit alle Texte aktualisiert werden, muss die Anwendung neu gestartet werden.\n\nMöchten Sie die Anwendung jetzt neustarten?\n\n(The language has been changed. Restart now to apply all changes?)",
                    "Ja / Yes",
                    "Nein / No"
                );

                if (restartNow)
                {
                    AppInstance.Restart(string.Empty);
                }
            }
        }

        partial void OnStartWithWindowsChanged(bool value)
        {
            if (_isInitializing) return;
            ToggleAutoStartAsync(value);
        }


        private async Task InitializeAsync()
        {
            _isInitializing = true;
            try
            {
                StartWithWindows = await _os.WindowsStartupManagerService.IsAutoStartEnabledAsync();

                if (_currentConfig.Settings.StartWithWindows && !StartWithWindows)
                {
                    await _os.WindowsStartupManagerService.EnableAutoStartAsync();
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void UpdateRestartUiState()
        {
            int days = SelectedComputerRestartOption?.Days ?? 0;

            IsRestartSliderVisible = days > 0;

            if (days == 0)
            {
                RestartStatusText = "Computer wird nicht neugestartet";
            }
            else
            {
                DateTime targetDate = _currentConfig.Computer.NextRestartDate ?? DateTime.MinValue;

                if (targetDate == DateTime.MinValue)
                {
                    targetDate = DateTime.Today.AddDays(days).AddHours(ComputerRestartClockTime);
                }

                RestartStatusText = $"Computer wird am {targetDate:dd.MM.yyyy} um {targetDate:HH} Uhr neugestartet";
            }
        }

        private void RecalculateNextRestartDate()
        {
            int days = _currentConfig.Computer.ComputerRestartIntervalDays;
            int hours = _currentConfig.Computer.RestartClockTime;

            if (days > 0)
            {
                _currentConfig.Computer.NextRestartDate = DateTime.Today.AddDays(days).AddHours(hours);
            }
            else
            {
                _currentConfig.Computer.NextRestartDate = DateTime.MinValue;
            }
        }

        private async void ToggleAutoStartAsync(bool enable)
        {
            if (enable)
            {
                await _os.WindowsStartupManagerService.EnableAutoStartAsync();
            }
            else
            {
                await _os.WindowsStartupManagerService.DisableAutoStartAsync();
            }

            _currentConfig.Settings.StartWithWindows = enable;
            SaveSettings();
        }

        private bool ValidateCredentials(string username, string domain, string password)
        {
            try
            {
                ContextType contextType = ContextType.Machine;

                if (!string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                {
                    contextType = ContextType.Domain;
                }

                using (var context = new PrincipalContext(contextType, domain))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch (PrincipalServerDownException)
            {
                throw new Exception("Der Domänen-Controller konnte zur Überprüfung nicht erreicht werden.");
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SaveSettings()
        {
            _eVisitorConfigService.SaveConfig(_currentConfig);
        }
    }
}
