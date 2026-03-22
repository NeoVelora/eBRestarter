using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelInstalledBrowsers : ObservableObject, IDisposable
    {
        #region FieldsAndInjectedServices

        private readonly IBrowserService _browserService;
        private readonly IBrowserDownloadService _downloadService;
        private readonly IDialogService _dialogService;
        private readonly IEVisitorConfigService _eVisitorConfigService;
        private readonly ILocalizationService _localizationService;
        private readonly IOperatingSystemFacade _os;
        private readonly DispatcherTimer _refreshTimer;

        #endregion

        #region ObservableProperties

        [ObservableProperty]
        public partial ObservableCollection<ViewModelBrowserItem> Browsers { get; set; } = [];

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelInstalledBrowsers(
            IBrowserService browserService,
            IBrowserDownloadService downloadService,
            IOperatingSystemFacade os,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IEVisitorConfigService eVisitorConfigService)
        {
            _downloadService = downloadService;
            _os = os;
            _browserService = browserService;
            _dialogService = dialogService;
            _localizationService = localizationService;
            _eVisitorConfigService = eVisitorConfigService;

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };

            _refreshTimer.Tick += async (__, _) => await LoadBrowsersSmartAsync();

            LoadBrowsersSmartAsync().Forget();

            _refreshTimer.Start();
        }

        #endregion

        #region PublicAndProtectedMethods

        public async Task LoadBrowsersSmartAsync()
        {
            var freshBrowserInfos = await _browserService.GetInstalledBrowsersAsync();

            foreach (var freshInfo in freshBrowserInfos)
            {
                var existingBrowserItem = Browsers.FirstOrDefault(browserItem => browserItem.BrowserType == freshInfo.Type);

                if (existingBrowserItem != null)
                {
                    existingBrowserItem.Update(freshInfo);
                }
                else
                {
                    var newBrowserItem = new ViewModelBrowserItem(
                        freshInfo,
                        _downloadService,
                        _os,
                        _eVisitorConfigService,
                        _dialogService,
                        _localizationService);
                    Browsers.Add(newBrowserItem);
                }
            }
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
