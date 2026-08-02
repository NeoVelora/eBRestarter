using System;
using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.SignalDTO.Messages;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelActivateApi : ObservableObject
{
    private const string HexColorError = "#E40E87";
    private const string HexColorSuccess = "#7ED422";
    private const string KeyActivateApiChecking = "ActivateApi_Checking";
    private const string KeyActivateApiFileNotFound = "ActivateApi_FileNotFound";
    private const string KeyActivateApiImportError = "ActivateApi_ImportError";
    private const string KeyActivateApiImportSuccess = "ActivateApi_ImportSuccess";
    private const string KeyActivateApiSuccess = "ActivateApi_Success";
    private const string SystemColorSuccessBrush = "{ThemeResource SystemFillColorSuccessBrush}";
    private const string TransparentColor = "Transparent";

    private readonly IOutboundPortApiAuthenticationProvider _apiAuthenticationService;
    private readonly IOutboundPortEVisitorConfigRepository _configService;
    private readonly IInboundPortLocalizationProvider _localizationService;


    public ViewModelActivateApi(
        IOutboundPortApiAuthenticationProvider apiAuthenticationService,
        IOutboundPortEVisitorConfigRepository configService,
        IInboundPortLocalizationProvider localizationService)
    {
        ArgumentNullException.ThrowIfNull(apiAuthenticationService);
        ArgumentNullException.ThrowIfNull(configService);
        ArgumentNullException.ThrowIfNull(localizationService);

        _apiAuthenticationService = apiAuthenticationService;
        _configService = configService;
        _localizationService = localizationService;

        var config = _configService.LoadConfig();
        Username = config?.Settings?.ApiUsername ?? string.Empty;
        ApiKey = config?.Settings?.ApiKey ?? string.Empty;
    }


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string ApiKey { get; set; } = string.Empty;

    private bool CanSubmit => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(ApiKey) && !IsBusy;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusColor { get; set; } = TransparentColor;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string Username { get; set; } = string.Empty;


    public void ImportLegacyFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            StatusMessage = _localizationService.RetrieveString(KeyActivateApiFileNotFound);
            StatusColor = HexColorError;
            return;
        }

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);
            var importedUser = reader.ReadString();
            var importedKey = reader.ReadString();
            Username = importedUser;
            ApiKey = importedKey;
            StatusMessage = _localizationService.RetrieveString(KeyActivateApiImportSuccess);
            StatusColor = SystemColorSuccessBrush;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            StatusMessage = _localizationService.RetrieveString(KeyActivateApiImportError);
            StatusColor = HexColorError;
            Username = string.Empty;
            ApiKey = string.Empty;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        IsBusy = true;
        StatusMessage = _localizationService.RetrieveString(KeyActivateApiChecking);

        try
        {
            var (isValid, verificationDetail) = await _apiAuthenticationService.VerifyCredentialsAsync(
                Username ?? string.Empty,
                ApiKey ?? string.Empty);

            if (!isValid)
            {
                StatusMessage = verificationDetail ?? string.Empty;
                StatusColor = HexColorError;
                return;
            }

            var currentConfig = _configService.LoadConfig();
            if (currentConfig?.Settings != null)
            {
                currentConfig.Settings.ApiUsername = Username ?? string.Empty;
                currentConfig.Settings.ApiKey = ApiKey ?? string.Empty;
                _configService.SaveConfig(currentConfig);
            }

            StatusMessage = _localizationService.RetrieveString(KeyActivateApiSuccess);
            StatusColor = HexColorSuccess;

            WeakReferenceMessenger.Default.Send(new ApiCredentialsUpdatedMessage());
        }
        finally
        {
            IsBusy = false;
        }
    }
}
