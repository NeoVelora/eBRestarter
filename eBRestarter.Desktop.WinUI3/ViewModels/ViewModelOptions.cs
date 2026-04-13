using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Interfaces.Update;
using eBRestarter.Core.Application.UseCases.ConfigureAutoLogon;
using eBRestarter.Core.Application.UseCases.ManageApplicationUpdates;
using eBRestarter.Core.Application.UseCases.RemoveApiCredentials;
using eBRestarter.Core.Application.UseCases.ToggleAppAutoStart;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Core.Domain.Models.Records.Config;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Infrastructure.Constants;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelOptions : ObservableObject
    {
        #region FieldsAndInjectedServices

        private readonly IConfigureAutoLogonUseCase _configureAutoLogonUseCase;
        private readonly IToggleAppAutoStartUseCase _toggleAppAutoStartUseCase;
        private readonly IManageApplicationUpdatesUseCase _manageApplicationUpdatesUseCase;
        private readonly IRemoveApiCredentialsUseCase _removeApiCredentialsUseCase;
        private readonly AppConfig _currentConfig;
        private readonly IDialogService _dialogService;
        private readonly IEVisitorConfigService _eVisitorConfigService;
        private bool _isInitializing = false;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly IOperatingSystemFacade _os;
        private readonly IRestartCalculationService _restartCalculationService;
        private readonly IThemeService _themeService;
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
        private readonly IComputerRestartScheduler _computerRestartScheduler;
        private readonly IBrowserExtensionDeploymentService _browserExtensionDeploymentService;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial ComputerRestartOption SelectedComputerRestartOption { get; set; }
        [ObservableProperty] public partial LanguageOption SelectedLanguageOption { get; set; }
        [ObservableProperty] public partial int ComputerRestartClockTime { get; set; }
        [ObservableProperty] public partial bool StartWithWindows { get; set; } = false;

        [ObservableProperty] public partial bool IsRestartSliderVisible { get; set; }
        [ObservableProperty] public partial string RestartStatusText { get; set; } = string.Empty;

        [ObservableProperty] public partial bool IsUpdateAvailable { get; set; }
        [ObservableProperty] public partial string UpdateMessage { get; set; } = string.Empty;

        [ObservableProperty] public partial string ExtensionUrl { get; set; } = string.Empty;
        [ObservableProperty] public partial double ExtensionWaitTimeMinutes { get; set; } = 3;
        [ObservableProperty] public partial LanguageOption SelectedExtensionLanguage { get; set; }
        [ObservableProperty] public partial string ExtensionSaveStatus { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
        public partial bool IsCheckingForUpdates { get; set; } = false;

        #endregion

        #region PublicProperties

        public ReadOnlyCollection<LanguageOption> LanguageList { get; private set; }
        public ReadOnlyCollection<ComputerRestartOption> ComputerRestartList { get; }
        public int ComputerRestartClockTimeMin { get; init; }
        public int ComputerRestartClockTimeMax { get; init; }

        public ReadOnlyCollection<LanguageOption> ExtensionLanguages { get; }

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelOptions(
            IDialogService dialogService,
            IConfigureAutoLogonUseCase configureAutoLogonUseCase,
            IToggleAppAutoStartUseCase toggleAppAutoStartUseCase,
            IManageApplicationUpdatesUseCase manageApplicationUpdatesUseCase,
            IRemoveApiCredentialsUseCase removeApiCredentialsUseCase,
            IOperatingSystemFacade os,
            IThemeService themeService,
            IEVisitorConfigService eVisitorConfigService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IRestartCalculationService restartCalculationService,
            IComputerRestartScheduler computerRestartScheduler,
            IBrowserExtensionDeploymentService browserExtensionDeploymentService)
        {
            _isInitializing = true;

            _dialogService = dialogService;
            _configureAutoLogonUseCase = configureAutoLogonUseCase;
            _toggleAppAutoStartUseCase = toggleAppAutoStartUseCase;
            _manageApplicationUpdatesUseCase = manageApplicationUpdatesUseCase;
            _removeApiCredentialsUseCase = removeApiCredentialsUseCase;
            _os = os;
            _themeService = themeService;
            _eVisitorConfigService = eVisitorConfigService;
            _languageService = languageService;
            _localizationService = localizationService;
            _restartCalculationService = restartCalculationService;
            _computerRestartScheduler = computerRestartScheduler;
            _browserExtensionDeploymentService = browserExtensionDeploymentService;

            _browserExtensionDeploymentService.EnsureExtensionIsDeployed();
            ComputerRestartList = new ReadOnlyCollection<ComputerRestartOption>([.. localizationService.GetComputerRestartOptions()]);

            LanguageList = new ReadOnlyCollection<LanguageOption>([.. localizationService.GetAvailableLanguages()]);

            _currentConfig = _eVisitorConfigService.LoadConfig();

            ComputerRestartClockTimeMin = 1;
            ComputerRestartClockTimeMax = 23;
            ComputerRestartClockTime = _currentConfig.Computer.RestartClockTime;

            var configDays = _currentConfig.Computer.ComputerRestartIntervalDays;
            var configLanguageIndex = _currentConfig.Settings.Language;

            SelectedComputerRestartOption = ComputerRestartList.FirstOrDefault(option => option.Days == configDays) ?? ComputerRestartList[0];
            SelectedLanguageOption = LanguageList.FirstOrDefault(option => option.Index == configLanguageIndex) ?? LanguageList[0];

            _isInitializing = false;

            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            _computerRestartScheduler.OnNextRestartDateChanged += OnNextRestartDateChanged;

            ExtensionLanguages = new ReadOnlyCollection<LanguageOption>([.. localizationService.GetAvailableLanguages()]);
            SelectedExtensionLanguage = ExtensionLanguages.FirstOrDefault()!;

            LoadExtensionConfig();

            InitializeAsync().Forget();


        }

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
        private async Task CheckForUpdates()
        {
            IsCheckingForUpdates = true;

            try
            {
                var response = await _manageApplicationUpdatesUseCase.CheckForUpdatesAsync();

                if (response.IsUpdateAvailable)
                {
                    IsUpdateAvailable = true;

                    string messageFormat = _localizationService.GetString("Options_UpdateAvailable");
                    UpdateMessage = string.Format(messageFormat, response.LatestVersion);

                    string title = _localizationService.GetString("Options_UpdateAvailable_Title");

                    string promptFormat = _localizationService.GetString("Options_UpdatePrompt_Message");
                    string dialogMessage = string.Format(promptFormat, UpdateMessage);

                    bool userWantsUpdate = await _dialogService.ShowConfirmationAsync(
                        title,
                        dialogMessage,
                        _localizationService.GetString("General_Yes"),
                        _localizationService.GetString("General_No")
                    );

                    if (userWantsUpdate)
                    {
                        await PerformUpdate();
                    }
                }
                else
                {
                    IsUpdateAvailable = false;
                    UpdateMessage = string.Empty;

                    await _dialogService.ShowMessageAsync(
                        _localizationService.GetString("Options_UpdateNoUpdate_Title"),
                        _localizationService.GetString("Options_UpdateNoUpdate_Message"),
                        DialogIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                string errorFormat = _localizationService.GetString("Options_UpdateCheckError_Message");
                string errorMessage = string.Format(errorFormat, ex.Message);

                await _dialogService.ShowMessageAsync(
                    _localizationService.GetString("Options_UpdateError_Title"),
                    errorMessage,
                    DialogIcon.Error
                );
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        [RelayCommand]
        private async Task PerformUpdate()
        {
            IsCheckingForUpdates = true;

            try
            {
                await _manageApplicationUpdatesUseCase.PerformUpdateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                string errorFormat = _localizationService.GetString("Options_UpdateFailed_Message");
                string errorMessage = string.Format(errorFormat, ex.Message);

                await _dialogService.ShowMessageAsync(
                    _localizationService.GetString("Options_UpdateFailed_Title"),
                    errorMessage,
                    DialogIcon.Error
                );
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        [RelayCommand]
        private void OpenExtensionFolder()
        {
            try
            {
                string extensionPath = _browserExtensionDeploymentService.GetExtensionFolderPath();
                if (!Directory.Exists(extensionPath))
                {
                    Directory.CreateDirectory(extensionPath);
                }
                _os.WindowsProcessControlService.OpenExplorer(extensionPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Öffnen des Extension-Ordners: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveExtensionConfig()
        {
            try
            {
                string extensionPath = _browserExtensionDeploymentService.GetExtensionFolderPath();
                string configPath = Path.Combine(extensionPath, "tab_restarter_config.json");

                var configData = new ExtensionConfigDto
                {
                    LANGUAGE = SelectedExtensionLanguage?.Index == 0 ? "DE" : "EN",
                    ZIEL_URL = $"{WebLinks.EVisitorSurflink}{_currentConfig.Username}",
                    WARTEZEIT_MS = (int)(ExtensionWaitTimeMinutes * 60000)
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,

                    TypeInfoResolver = ExtensionConfigJsonContext.Default
                };

                string jsonString = JsonSerializer.Serialize(configData, options);

                if (!Directory.Exists(_browserExtensionDeploymentService.GetExtensionFolderPath())) Directory.CreateDirectory(_browserExtensionDeploymentService.GetExtensionFolderPath());
                await File.WriteAllTextAsync(configPath, jsonString);

                ExtensionSaveStatus = _localizationService.GetString("Options_SavedSuccessfully") ?? "Erfolgreich gespeichert!";
                await Task.Delay(3000);
                ExtensionSaveStatus = string.Empty;
            }
            catch (Exception ex)
            {

                await _dialogService.ShowMessageAsync(_localizationService.GetString("General_Error"), _localizationService.GetString("BrowserExtension_Config_Error_Message") + " " + ex.Message, DialogIcon.Error);
            }
        }

        [JsonSerializable(typeof(ExtensionConfigDto))]
        public partial class ExtensionConfigJsonContext : JsonSerializerContext
        {
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
            _removeApiCredentialsUseCase.Execute();

            _currentConfig.Settings.ApiUsername = string.Empty;
            _currentConfig.Settings.ApiKey = string.Empty;

            WeakReferenceMessenger.Default.Send(new ApiCredentialsRemovedMessage());

            await _dialogService.ShowMessageAsync(
                _localizationService.GetString("Options_RemoveCreds_Title"),
                _localizationService.GetString("Options_RemoveCreds_Message"),
                DialogIcon.Success);
        }

        [RelayCommand]
        private async Task ConfigureAutoLogon()
        {
            string currentUser = Environment.UserName;
            string currentDomain = Environment.UserDomainName;

            var dialogResult = await _dialogService.ShowAutoLogonDialogAsync(currentUser, currentDomain);

            if (dialogResult == null) return;

            var request = new ConfigureAutoLogonRequest(
                IsDeactivateAction: dialogResult.IsDeactivateAction,
                Username: dialogResult.Credentials?.Username,
                Domain: dialogResult.Credentials?.Domain,
                Password: dialogResult.Credentials?.Password
            );

            var response = _configureAutoLogonUseCase.Execute(request);

            if (response.Success)
            {
                if (response.Status == AutoLogonResultStatus.Deactivated)
                {
                    await _dialogService.ShowMessageAsync("Info", _localizationService.GetString("Options_AutoLogon_Deactivated"));
                }
                else if (response.Status == AutoLogonResultStatus.Activated)
                {
                    await _dialogService.ShowMessageAsync(_localizationService.GetString("General_Success"), _localizationService.GetString("Options_AutoLogon_Success"));
                }
            }
            else
            {
                if (response.Status == AutoLogonResultStatus.WindowsHelloBlockActive)
                {
                    string title = _localizationService.GetString("Options_AutoLogon_WindowsHelloErrorTitle");
                    string message = _localizationService.GetString("Options_AutoLogon_WindowsHelloErrorMessage");

                    await _dialogService.ShowMessageAsync(title, message, DialogIcon.Error);
                }
                else if (response.Status == AutoLogonResultStatus.ValidationError)
                {
                    await _dialogService.ShowMessageAsync(_localizationService.GetString("General_Error"), _localizationService.GetString("Options_AutoLogon_ValidationError"));
                }
                else if (response.Status == AutoLogonResultStatus.DomainError)
                {
                    await _dialogService.ShowMessageAsync(_localizationService.GetString("General_Error"), _localizationService.GetString("Options_AutoLogon_DomainError"));
                }
                else
                {
                    string errorFormat = _localizationService.GetString("General_UnexpectedError");
                    await _dialogService.ShowMessageAsync(_localizationService.GetString("General_Error"), string.Format(errorFormat, response.ErrorMessage));
                }
            }
        }

        #endregion

        #region PropertyChangeHandlers

        private void OnNextRestartDateChanged(object? sender, DateTime? newDate)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                _currentConfig.Computer.NextRestartDate = newDate;
                UpdateRestartUiState();
            });
        }


        partial void OnSelectedComputerRestartOptionChanged(ComputerRestartOption value)
        {
            if (value == null || _isInitializing)
            {
                UpdateRestartUiState();
                return;
            }

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
                    "Ja / Yes", "Nein / No"
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
            ToggleAutoStartAsync(value).Forget();
        }

        #endregion

        #region PrivateHelperMethods


        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        private void LoadExtensionConfig()
        {
            try
            {
                string configPath = Path.Combine(_browserExtensionDeploymentService.GetExtensionFolderPath(), "tab_restarter_config.json");

                if (File.Exists(configPath))
                {
                    string jsonString = File.ReadAllText(configPath);

                    var configData = JsonSerializer.Deserialize<ExtensionConfigDto>(jsonString);

                    if (configData != null)
                    {
                        ExtensionUrl = configData.ZIEL_URL;
                        ExtensionWaitTimeMinutes = configData.WARTEZEIT_MS / 60000.0;

                        if (configData.LANGUAGE == "EN")
                            SelectedExtensionLanguage = ExtensionLanguages.FirstOrDefault(l => l.Index == 1)!;
                        else
                            SelectedExtensionLanguage = ExtensionLanguages.FirstOrDefault(l => l.Index == 0)!;
                    }
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"Konnte existierende config.json nicht lesen: {ex.Message}");
            }
        }

        private bool CanCheckForUpdates() => !IsCheckingForUpdates;

        private void SaveThemeConfig(string theme)
        {
            var config = _eVisitorConfigService.LoadConfig();
            var newConfig = config with { Settings = config.Settings with { Theme = theme } };

            _eVisitorConfigService.SaveConfig(newConfig);
        }

        private async Task InitializeAsync()
        {
            _isInitializing = true;

            try
            {
                StartWithWindows = await _toggleAppAutoStartUseCase.InitializeAndGetStateAsync();
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
                RestartStatusText = _localizationService.GetString("Options_RestartStatus_None");
            }
            else
            {
                DateTime targetDate = _currentConfig.Computer.NextRestartDate ?? DateTime.MinValue;

                if (targetDate == DateTime.MinValue)
                {
                    targetDate = DateTime.Today.AddDays(days).AddHours(ComputerRestartClockTime);
                }

                string messageFormat = _localizationService.GetString("Options_RestartStatus_Scheduled");

                RestartStatusText = string.Format(messageFormat, targetDate.ToString("dd.MM.yyyy"), targetDate.ToString("HH"));
            }
        }

        private void RecalculateNextRestartDate()
        {
            int days = _currentConfig.Computer.ComputerRestartIntervalDays;
            int hours = _currentConfig.Computer.RestartClockTime;

            _currentConfig.Computer.NextRestartDate = _restartCalculationService.GetNextRestartDate(days, hours);
        }

        private async Task ToggleAutoStartAsync(bool enable)
        {
            await _toggleAppAutoStartUseCase.ToggleAsync(enable);

            _currentConfig.Settings.StartWithWindows = enable;
        }

        private void SaveSettings()
        {
            _eVisitorConfigService.SaveConfig(_currentConfig);
        }

        #endregion
    }
}
