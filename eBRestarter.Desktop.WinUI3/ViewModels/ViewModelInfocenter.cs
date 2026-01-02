using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Desktop.WinUI3.Services;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelInfocenter : ObservableObject
    {
        private readonly IHardwareInfoService _hardwareService;
        private readonly IOsEditionService _osEditionService;
        private readonly IWindowsSystemInfoService _systemInfoService;
        private readonly IDialogService _dialogService;

        [ObservableProperty] public partial string ProcessorText { get; set; } = "Lade...";
        [ObservableProperty] public partial string GraphicsText { get; set; } = "Lade...";
        [ObservableProperty] public partial string RamText { get; set; } = "Lade...";

        [ObservableProperty] public partial string OsEditionText { get; set; } = "Lade...";
        [ObservableProperty] public partial string OsVersionText { get; set; } = "Lade...";
        [ObservableProperty] public partial string OsBuildText { get; set; } = "Lade...";
        [ObservableProperty] public partial string BrowserText { get; set; } = "Lade...";

        public string Title { get; } = "Infocenter";

        public ViewModelInfocenter(
            IHardwareInfoService hardwareService,
            IOsEditionService osEditionService,
            IWindowsSystemInfoService systemInfoService,
            IDialogService dialogService)
        {
            _hardwareService = hardwareService;
            _osEditionService = osEditionService;
            _systemInfoService = systemInfoService;
            _dialogService = dialogService;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task ShowAboutInfo()
        {
            await _dialogService.ShowAboutDialogAsync();
        }


        private async Task LoadDataAsync()
        {
            var hardware = await _hardwareService.GetHardwareInfoAsync();

            ProcessorText = $"Prozessor: {hardware.ProcessorName}";
            GraphicsText = $"Grafikkarte: {hardware.GraphicsCardName}";
            RamText = $"Installierter RAM: {hardware.InstalledRam}";

            var edition = await _osEditionService.GetOsEditionAsync();
            OsEditionText = $"Edition: {edition}";

            OsVersionText = $"Version: {_systemInfoService.GetCurrentOsDisplayVersion()}";
            OsBuildText = $"Betriebssystembuild: {_systemInfoService.GetCurrentOsBuildVersion()}";
            BrowserText = $"Standardbrowser: {_systemInfoService.GetCurrentStandardBrowserName()}";
        }

        [RelayCommand]
        public void OpenSupportWebsite(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch {}
            }
        }
    }
}
