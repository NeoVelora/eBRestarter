using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Contstants;
using eBRestarter.Core.Application.Facade;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Models.Records.Config;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelRestarterProperties : ObservableObject
    {
        private readonly IOperatingSystemFacade _operatingSystemFacade;
        private readonly IEVisitorConfigService _eVisitorConfigService;

        private readonly AppConfig _currentConfig;

        public int RuntimePauseSecondsMin { get; init; }
        public int RuntimePauseSecondsMax { get; init; }
        public int BrowserRuntimeHoursMin { get; init; }
        public int BrowserRuntimeHoursMax { get; init; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddeVVisitorUsernameCommand))]
        public partial string Username { get; set; } = string.Empty;

        [ObservableProperty] private partial string StandardBrowser { get; set; } = string.Empty;

        [ObservableProperty] public partial int RuntimePauseSeconds { get; set; }

        [ObservableProperty] public partial int RuntimeHours { get; set; }

        public ViewModelRestarterProperties(IOperatingSystemFacade operatingSystemFacade, IEVisitorConfigService eVisitorConfigService)
        {
            _operatingSystemFacade = operatingSystemFacade;
            _eVisitorConfigService = eVisitorConfigService;

            _currentConfig = _eVisitorConfigService.LoadConfig();

            RuntimePauseSecondsMin = 20;
            RuntimePauseSecondsMax = 60;
            BrowserRuntimeHoursMin = 1;
            BrowserRuntimeHoursMax = 12;

            RuntimePauseSeconds = _currentConfig.Browser.RuntimePauseSeconds;
            RuntimeHours = _currentConfig.Browser.RuntimeHours;

        }

        [RelayCommand(CanExecute = nameof(CanAddUsername))]
        private void AddeVVisitorUsername()
        {
            _currentConfig.Username = Username;

            Username = string.Empty;
        }

        private bool CanAddUsername()
        {
            return !string.IsNullOrWhiteSpace(Username);
        }

        [RelayCommand]
        public void RegisterToEVisitor()
        {
            _operatingSystemFacade.WindowsProcessControlService.OpenUrlInBrowser(WebLinks.RegistrationLink);
        }


        partial void OnRuntimePauseSecondsChanged(int value)
        {
            int clampedValue = Math.Clamp(value, RuntimePauseSecondsMin, RuntimePauseSecondsMax);

            Debug.WriteLine($"RestMinutes geändert: {value} -> Korrigiert auf: {clampedValue}");

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

        partial void OnRuntimeHoursChanged(int value)
        {
            int clampedValue = Math.Clamp(value, BrowserRuntimeHoursMin, BrowserRuntimeHoursMin);

            Debug.WriteLine($"RestMinutes geändert: {value} -> Korrigiert auf: {clampedValue}");

            if (value != clampedValue)
            {
                RuntimeHours = clampedValue;
                return;
            }

            if (_currentConfig.Browser.RuntimeHours != value)
            {
                RuntimeHours = value;
                _currentConfig.Browser.RuntimeHours = value;
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            _eVisitorConfigService.SaveConfig(_currentConfig);
        }
    }
}
