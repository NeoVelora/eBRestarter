using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces.Authentication;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public partial class ViewModelImportApi : ObservableObject
{
    private const string BaseIconUri = "ms-appx:///Resources/Visuals/Icons/";

    private const string DefaultFileStatusIconUri = BaseIconUri + "LightTheme/note_light_theme.png";

    private const string ErrorFileReadMessage = "Fehler beim Lesen der Datei (Format ungültig).";

    private const string ErrorInvalidFileFormatMessage = "Ungültiges Dateiformat. Bitte .apiaf Datei verwenden.";

    private const string FileStatusIconErrorUri = BaseIconUri + "Intersection/wrong_document.png";

    private const string FileStatusIconReadyUri = BaseIconUri + "Intersection/approval.png";

    private const string ImportFailedMessagePrefix = "Import fehlgeschlagen: ";

    private const string ImportVerificationBusyMessage = "Lese Datei und prüfe Zugangsdaten...";

    private const string InvalidStatusColorHex = "#E40E87";

    private const string LegacyApiFileExtension = ".apiaf";

    private const string ReadyStatusColorThemeKey = "{ThemeResource TextFillColorPrimaryBrush}";

    private const string StatusColorThemeCautionKey = "{ThemeResource SystemFillColorCautionBrush}";

    private const string StatusFileReadyMessage = "Datei erkannt. Bereit zum Import.";

    private const string SuccessImportMessage = "Import und Aktivierung erfolgreich!";

    private const string SuccessStatusColorHex = "#7ED422";

    private const string UnexpectedImportErrorMessage = "Unerwarteter Fehler beim Import.";

    private readonly IApiAuthenticationService _authService;

    private readonly ICredentialStore _credentialStore;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }


    [ObservableProperty]
    public partial string FileStatusIcon { get; set; } = DefaultFileStatusIconUri;

    [ObservableProperty]
    public partial string ImportedFileName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    public partial string ImportedFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusColor { get; set; } = "Transparent";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    private bool CanImport => !string.IsNullOrEmpty(ImportedFilePath) && !IsBusy;

    public ViewModelImportApi(IApiAuthenticationService authService, ICredentialStore credentialStore)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(credentialStore);

        _authService = authService;
        _credentialStore = credentialStore;
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task Import()
    {
        IsBusy = true;

        try
        {
            StatusMessage = ImportVerificationBusyMessage;
            StatusColor = StatusColorThemeCautionKey;

            var credentials = _credentialStore.ImportFromLegacyFile(ImportedFilePath);

            if (credentials == null)
            {
                StatusMessage = ErrorFileReadMessage;
                StatusColor = InvalidStatusColorHex;
                return;
            }

            var (isValid, verificationDetail) = await _authService.VerifyCredentialsAsync(credentials.Username, credentials.ApiKey);

            if (isValid)
            {
                _credentialStore.SaveCredentials(credentials);
                StatusMessage = SuccessImportMessage;
                StatusColor = SuccessStatusColorHex;
            }
            else
            {
                StatusMessage = $"{ImportFailedMessagePrefix}{verificationDetail}";
                StatusColor = InvalidStatusColorHex;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            StatusMessage = UnexpectedImportErrorMessage;
            StatusColor = InvalidStatusColorHex;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void HandleFileDrop(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            StatusMessage = ErrorInvalidFileFormatMessage;
            StatusColor = InvalidStatusColorHex;
            FileStatusIcon = FileStatusIconErrorUri;
            ImportedFilePath = string.Empty;
            ImportedFileName = string.Empty;
            return;
        }

        if (!filePath.EndsWith(LegacyApiFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = ErrorInvalidFileFormatMessage;
            StatusColor = InvalidStatusColorHex;
            FileStatusIcon = FileStatusIconErrorUri;
            ImportedFilePath = string.Empty;
            ImportedFileName = string.Empty;
            return;
        }

        ImportedFilePath = filePath;
        ImportedFileName = Path.GetFileName(filePath);
        StatusMessage = StatusFileReadyMessage;
        StatusColor = ReadyStatusColorThemeKey;
        FileStatusIcon = FileStatusIconReadyUri;
    }
}
