using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.UseCases.GetSystemInformation;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using System.Diagnostics;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelInfocenter : ObservableObject
    {
        #region FieldsAndInjectedServices

        private readonly IGetSystemInformationUseCase _getSystemInformationUseCase;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial string BrowserText { get; set; }
        [ObservableProperty] public partial string GraphicsText { get; set; }
        [ObservableProperty] public partial string OsBuildText { get; set; }
        [ObservableProperty] public partial string OsEditionText { get; set; }
        [ObservableProperty] public partial string OsVersionText { get; set; }
        [ObservableProperty] public partial string ProcessorText { get; set; }
        [ObservableProperty] public partial string RamText { get; set; }

        #endregion

        #region PublicProperties

        public string? Title { get; private set; }

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelInfocenter(
            IGetSystemInformationUseCase getSystemInformationUseCase,
            IDialogService dialogService,
            ILocalizationService localizationService)
        {
            _getSystemInformationUseCase = getSystemInformationUseCase;
            _dialogService = dialogService;
            _localizationService = localizationService;

            string loading = _localizationService.GetString("Infocenter_Loading");
            BrowserText = loading;
            GraphicsText = loading;
            OsBuildText = loading;
            OsEditionText = loading;
            OsVersionText = loading;
            ProcessorText = loading;
            RamText = loading;

            _ = LoadDataAsync();
        }

        #endregion

        #region Commands

        [RelayCommand]
        public void OpenSupportWebsite(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { }
            }
        }

        [RelayCommand]
        private async Task ShowAboutInfo()
        {
            await _dialogService.ShowAboutDialogAsync();
        }

        #endregion

        #region PrivateHelperMethods

        private async Task LoadDataAsync()
        {
            var response = await _getSystemInformationUseCase.ExecuteAsync();

            ProcessorText = $"{_localizationService.GetString("Infocenter_ProcessorPrefix")} {response.ProcessorName}";
            GraphicsText = $"{_localizationService.GetString("Infocenter_GraphicsPrefix")} {response.GraphicsCardName}";
            RamText = $"{_localizationService.GetString("Infocenter_RamPrefix")} {response.InstalledRam}";

            OsEditionText = $"{_localizationService.GetString("Infocenter_EditionPrefix")} {response.OsEdition}";

            OsVersionText = $"{_localizationService.GetString("Infocenter_VersionPrefix")} {response.OsDisplayVersion}";
            OsBuildText = $"{_localizationService.GetString("Infocenter_BuildPrefix")} {response.OsBuildVersion}";
            BrowserText = $"{_localizationService.GetString("Infocenter_BrowserPrefix")} {response.StandardBrowserName}";
        }

        #endregion
    }
}
