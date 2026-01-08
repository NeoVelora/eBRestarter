using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces.Authentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ImportApiViewModel : ObservableObject
    {
        private readonly IApiAuthenticationService _authService;
        private readonly ICredentialStore _credentialStore;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
        private string _importedFilePath = "";

        [ObservableProperty] public partial string ImportedFileName { get; set; } = "";

        [ObservableProperty] public partial string FileStatusIcon { get; set; } = "ms-appx:///Assets/Images/Icons/Intersection/AddFileIcon.png";

        [ObservableProperty] public partial bool IsBusy { get; set; }
        [ObservableProperty] public partial string StatusMessage { get; set; } = "";
        [ObservableProperty] public partial string StatusColor { get; set; } = "Transparent";

        public ImportApiViewModel(
            IApiAuthenticationService authService,
            ICredentialStore credentialStore)
        {
            _authService = authService;
            _credentialStore = credentialStore;
        }

        public void HandleFileDrop(string filePath)
        {
            if (!filePath.EndsWith(".apiaf"))
            {
                StatusMessage = "Ungültiges Dateiformat. Bitte .apiaf Datei verwenden.";
                StatusColor = "#E40E87";
                FileStatusIcon = "ms-appx:///Assets/Images/Icons/Intersection/wrong_document.png";
                ImportedFilePath = "";
                ImportedFileName = "";
                return;
            }

            ImportedFilePath = filePath;
            ImportedFileName = System.IO.Path.GetFileName(filePath);
            StatusMessage = "Datei erkannt. Bereit zum Import.";
            StatusColor = "{ThemeResource TextFillColorPrimaryBrush}";
            FileStatusIcon = "ms-appx:///Assets/Images/Icons/Intersection/approval.png";
        }

        public void HandleFileSelect()
        {
        }

        private bool CanImport => !string.IsNullOrEmpty(ImportedFilePath) && !IsBusy;

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

            var result = await _authService.VerifyCredentialsAsync(credentials.Username, credentials.ApiKey);

            IsBusy = false;

            if (result.IsValid)
            {
                _credentialStore.SaveCredentials(credentials);

                StatusMessage = "Import und Aktivierung erfolgreich!";
                StatusColor = "#7ED422";
            }
            else
            {
                StatusMessage = $"Import fehlgeschlagen: {result.Message}";
                StatusColor = "#E40E87";
            }
        }
    }
}
