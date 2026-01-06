using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces.Authentication;
using eBRestarter.Core.Domain.Models.Records;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ActivateApiViewModel : ObservableObject
    {
        private readonly IApiAuthenticationService _authService;
        private readonly ICredentialStore _credentialStore;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        public partial string Username { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
        public partial string ApiKey { get; set; } = string.Empty;

        [ObservableProperty] public partial bool IsBusy { get; set; }
        [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;
        [ObservableProperty] public partial string StatusColor { get; set; } = "Transparent";

        public ActivateApiViewModel(
            IApiAuthenticationService authService,
            ICredentialStore credentialStore)
        {
            _authService = authService;
            _credentialStore = credentialStore;

            var saved = _credentialStore.LoadCredentials();
            if (saved != null)
            {
                Username = saved.Username;
                ApiKey = saved.ApiKey;
            }
        }

        private bool CanSubmit => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(ApiKey) && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private async Task Submit()
        {
            IsBusy = true;
            StatusMessage = "Prüfe Zugangsdaten...";
            StatusColor = "{ThemeResource SystemFillColorCautionBrush}";

            var result = await _authService.VerifyCredentialsAsync(Username, ApiKey);

            IsBusy = false;

            if (result.IsValid)
            {
                _credentialStore.SaveCredentials(new ApiCredentials(Username, ApiKey));

                StatusMessage = "Erfolgreich aktiviert!";
                StatusColor = "#7ED422";
            }
            else
            {
                StatusMessage = result.Message;
                StatusColor = "#E40E87";
            }
        }

        public void ImportLegacyFile(string filePath)
        {
            var imported = _credentialStore.ImportFromLegacyFile(filePath);
            if (imported != null)
            {
                Username = imported.Username;
                ApiKey = imported.ApiKey;
                StatusMessage = "Daten aus Datei importiert. Bitte 'Aktivieren' klicken.";
                StatusColor = "{ThemeResource SystemFillColorSuccessBrush}";
            }
            else
            {
                StatusMessage = "Fehler beim Lesen der Datei.";
                StatusColor = "#E40E87";
            }
        }
    }
}
