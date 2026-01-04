using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Infrastructure.Browsers.Abstract;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelDeleteBrowserContent : ObservableObject
    {
        private readonly IBrowserFactory _browserFactory;
        private readonly IWindowsProcessControlService _processService;
        private readonly IFileDeletionService _fileDeletionService;
        private readonly IDialogService _dialogService;

        private IBrowser? _currentBrowser;
        private BrowserPaths? _browserPaths;
        private string _processName = "";

        private CancellationTokenSource? _cts;

        [ObservableProperty] public partial string BrowserName { get; set; } = "Lade...";
        [ObservableProperty] public partial string BrowserIconPath { get; set; }  = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
        public partial bool IsDeleteCookiesChecked { get; set; } = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
        public partial bool IsDeleteInternetCacheChecked { get; set; } = true;

        [ObservableProperty] public partial double ProgressValue { get; set; } = 0;
        [ObservableProperty] public partial double ProgressMaximum { get; set; } = 100;
        [ObservableProperty] public partial string ProgressText { get; set; } = "0 %";
        [ObservableProperty] public partial string StatusText { get; set; } = "Bereit.";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCleaningCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelCleaningCommand))]
        public partial bool IsBusy { get; set; } = false;

        public ViewModelDeleteBrowserContent(
            IBrowserFactory browserFactory,
            IWindowsProcessControlService processService,
            IFileDeletionService fileDeletionService,
            IDialogService dialogService)
        {
            _browserFactory = browserFactory;
            _processService = processService;
            _fileDeletionService = fileDeletionService;
            _dialogService = dialogService;
        }

        public void Initialize(BrowserType selectedBrowserType)
        {
            try
            {
                _currentBrowser = _browserFactory.Create(selectedBrowserType);

                BrowserName = _currentBrowser.DisplayName;
                BrowserIconPath = FixIconPathForWinUI(_currentBrowser.IconPath);

                _browserPaths = _currentBrowser.GetPaths();

                _processName = GetProcessNameByType(selectedBrowserType);
            }
            catch (Exception ex)
            {
                StatusText = $"Fehler beim Laden: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanClean))]
        private async Task StartCleaning()
        {
            if (IsBusy || _currentBrowser == null || _browserPaths == null) return;

            if (_processService.IsProcessAlive(_processName))
            {
                var result = await _dialogService.ShowYesNoDialogAsync(
                    "Browser schließen?",
                    $"Der Browser '{BrowserName}' läuft noch. Er muss beendet werden, um zu bereinigen.");

                if (result)
                {
                    _processService.CloseApplication(_processName);
                    await Task.Delay(1000);

                    if (_processService.IsProcessAlive(_processName))
                    {
                        StatusText = "Konnte Browser nicht beenden.";
                        return;
                    }
                }
                else
                {
                    StatusText = "Abbruch durch Benutzer.";
                    return;
                }
            }

            var directoriesToDelete = new List<string>();

            if (IsDeleteInternetCacheChecked && !string.IsNullOrEmpty(_browserPaths.CacheDir))
                directoriesToDelete.Add(_browserPaths.CacheDir);

            if (IsDeleteCookiesChecked && !string.IsNullOrEmpty(_browserPaths.CookiesDir))
                directoriesToDelete.Add(_browserPaths.CookiesDir);


            if (directoriesToDelete.Count == 0)
            {
                StatusText = "Keine gültigen Pfade gefunden.";
                return;
            }

            IsBusy = true;
            _cts = new CancellationTokenSource();

            try
            {
                StatusText = "Analysiere Dateien...";

                int totalFiles = await _fileDeletionService.CountFilesAsync(directoriesToDelete);
                ProgressMaximum = totalFiles > 0 ? totalFiles : 1;
                ProgressValue = 0;

                var statusProgress = new Progress<string>(msg => StatusText = msg);
                var valueProgress = new Progress<int>(val =>
                {
                    ProgressValue = val;
                    if (totalFiles > 0)
                        ProgressText = $"{(val * 100 / totalFiles)} %";
                });

                await _fileDeletionService.DeleteFilesAsync(directoriesToDelete, statusProgress, valueProgress, _cts.Token);


                StatusText = "Bereinigung abgeschlossen.";
            }
            catch (Exception ex)
            {
                StatusText = $"Fehler: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                _cts = null;
            }
        }

        [RelayCommand(CanExecute = nameof(CanCancel))]
        private void CancelCleaning()
        {
            _cts?.Cancel();
            StatusText = "Breche ab...";
        }

        private bool CanClean() => !IsBusy && (IsDeleteCookiesChecked || IsDeleteInternetCacheChecked);
        private bool CanCancel() => IsBusy;

        private string FixIconPathForWinUI(string path)
        {
            return path.Replace("/Resources/Visuals", "ms-appx:///Assets/Visuals");
        }

        private string GetProcessNameByType(BrowserType type)
        {
            return type switch
            {
                BrowserType.Chrome => "chrome",
                BrowserType.Firefox => "firefox",
                BrowserType.Edge => "msedge",
                BrowserType.Brave => "brave",
                _ => ""
            };
        }
    }
}
