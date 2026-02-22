using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.Timers;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelInstallAddOn : ObservableObject, IDisposable
    {
        #region FieldsAndInjectedServices

        private readonly IBrowserFactory _browserFactory;
        private readonly Timer _timer;

        #endregion

        #region PublicProperties

        public ObservableCollection<ViewModelBrowserAddonStatus> Browsers { get; } = [];

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelInstallAddOn(IBrowserFactory browserFactory, ILocalizationService localizationService)
        {
            _browserFactory = browserFactory;

            Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Chrome), localizationService));
            Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Firefox), localizationService));
            Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Edge), localizationService));
            Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Brave), localizationService));
            Browsers.Add(new ViewModelBrowserAddonStatus(_browserFactory.Create(BrowserType.Vivaldi), localizationService));

            _timer = new Timer(2000);
            _timer.Elapsed += (s, e) =>
            {
                foreach (var browserAddonStatus in Browsers) browserAddonStatus.RefreshStatus();
            };
            _timer.Start();
        }

        #endregion

        #region PublicAndProtectedMethods

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        #endregion
    }
}
