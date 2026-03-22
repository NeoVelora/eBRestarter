using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces.Authentication;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelImportApi(
        IApiAuthenticationService authService,
        ICredentialStore credentialStore) : ObservableObject
    {
        #region FieldsAndInjectedServices

        private readonly IApiAuthenticationService _authService = authService;
        private readonly ICredentialStore _credentialStore = credentialStore;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial string FileStatusIcon { get; set; } = "ms-appx:///Resources/Visuals/Icons/LightTheme/note_light_theme.png";
        [ObservableProperty] public partial string ImportedFileName { get; set; } = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
        public partial string ImportedFilePath { get; set; } = string.Empty;
        [ObservableProperty] public partial bool IsBusy { get; set; }
        [ObservableProperty] public partial string StatusColor { get; set; } = "Transparent";
        [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanImport))]
        private async Task Import()
        {
            IsBusy = true;
            StatusMessage = "Lese Datei und prüfe Zugangsdaten...";
            StatusColor = "{ThemeResource SystemFillColorCautionBrush}";

            var credentials = _credentialStore.ImportFromLegacyFile(ImportedFilePath);

            if (credentials == null)
            {
                StatusMessage = "Fehler beim Lesen der Datei (Format ungültig).";
                StatusColor = "#E40E87";
                IsBusy = false;
                return;
            }

            var (IsValid, Message) = await _authService.VerifyCredentialsAsync(credentials.Username, credentials.ApiKey);

            IsBusy = false;

            if (IsValid)
            {
                _credentialStore.SaveCredentials(credentials);
                StatusMessage = "Import und Aktivierung erfolgreich!";
                StatusColor = "#7ED422";
            }
            else
            {
                StatusMessage = $"Import fehlgeschlagen: {Message}";
                StatusColor = "#E40E87";
            }
        }

        #endregion

        #region PublicAndProtectedMethods

        public void HandleFileDrop(string filePath)
        {
            if (!filePath.EndsWith(".apiaf"))
            {
                StatusMessage = "Ungültiges Dateiformat. Bitte .apiaf Datei verwenden.";
                StatusColor = "#E40E87";
                FileStatusIcon = "ms-appx:///Resources/Visuals/Icons/Intersection/wrong_document.png";
                ImportedFilePath = "";
                ImportedFileName = "";
                return;
            }
            ImportedFilePath = filePath;
            ImportedFileName = System.IO.Path.GetFileName(filePath);
            StatusMessage = "Datei erkannt. Bereit zum Import.";
            StatusColor = "{ThemeResource TextFillColorPrimaryBrush}";
            FileStatusIcon = "ms-appx:///Resources/Visuals/Icons/Intersection/approval.png";
        }

        #endregion

        #region PrivateHelperMethods

        private bool CanImport => !string.IsNullOrEmpty(ImportedFilePath) && !IsBusy;

        #endregion
    }
}
