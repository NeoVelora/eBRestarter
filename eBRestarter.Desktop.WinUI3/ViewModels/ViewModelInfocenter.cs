using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelInfocenter : ObservableObject
{
    private const string InfocenterLoadFailedResourceKey = "Infocenter_LoadFailed";

    private const string InfocenterLoadingResourceKey = "Infocenter_Loading";

    private readonly IDialogService _dialogService;

    private readonly IInboundPortSystemInformationProvider _systemInformationProvider;

    private readonly IInboundPortLocalizationProvider _localizationService;

    [ObservableProperty]
    public partial string BrowserText { get; set; }

    [ObservableProperty]
    public partial string GraphicsText { get; set; }

    [ObservableProperty]
    public partial string OsBuildText { get; set; }

    [ObservableProperty]
    public partial string OsEditionText { get; set; }

    [ObservableProperty]
    public partial string OsVersionText { get; set; }

    [ObservableProperty]
    public partial string ProcessorText { get; set; }

    [ObservableProperty]
    public partial string RamText { get; set; }

    public ViewModelInfocenter(
        IInboundPortSystemInformationProvider systemInformationProvider,
        IDialogService dialogService,
        IInboundPortLocalizationProvider LocalizationProvider)
    {
        ArgumentNullException.ThrowIfNull(systemInformationProvider);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);

        _systemInformationProvider = systemInformationProvider;
        _dialogService = dialogService;
        _localizationService = LocalizationProvider;

        string loadingPlaceholder = _localizationService.RetrieveString(InfocenterLoadingResourceKey);
        BrowserText = loadingPlaceholder;
        GraphicsText = loadingPlaceholder;
        OsBuildText = loadingPlaceholder;
        OsEditionText = loadingPlaceholder;
        OsVersionText = loadingPlaceholder;
        ProcessorText = loadingPlaceholder;
        RamText = loadingPlaceholder;

        _ = LoadDataAsync();
    }

    [RelayCommand]
    public static void OpenSupportWebsite(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task ShowAboutInfo()
    {
        await _dialogService.ShowAboutDialogAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var systemInformation = await _systemInformationProvider.RetrieveAsync();

            ProcessorText = $"{_localizationService.RetrieveString("Infocenter_ProcessorPrefix")} {systemInformation.ProcessorName}";
            GraphicsText = $"{_localizationService.RetrieveString("Infocenter_GraphicsPrefix")} {systemInformation.GraphicsCardName}";
            RamText = $"{_localizationService.RetrieveString("Infocenter_RamPrefix")} {systemInformation.InstalledRam}";
            OsEditionText = $"{_localizationService.RetrieveString("Infocenter_EditionPrefix")} {systemInformation.OsEdition}";
            OsVersionText = $"{_localizationService.RetrieveString("Infocenter_VersionPrefix")} {systemInformation.OsDisplayVersion}";
            OsBuildText = $"{_localizationService.RetrieveString("Infocenter_BuildPrefix")} {systemInformation.OsBuildVersion}";
            BrowserText = $"{_localizationService.RetrieveString("Infocenter_BrowserPrefix")} {systemInformation.StandardBrowserName}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            string failureMessage = _localizationService.RetrieveString(InfocenterLoadFailedResourceKey);
            BrowserText = failureMessage;
            GraphicsText = failureMessage;
            OsBuildText = failureMessage;
            OsEditionText = failureMessage;
            OsVersionText = failureMessage;
            ProcessorText = failureMessage;
            RamText = failureMessage;
        }
    }
}


