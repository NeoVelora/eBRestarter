using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Desktop.WinUI3.Helpers;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;


namespace eBRestarter.Desktop.WinUI3
{
    public sealed partial class EBRestarter : Window
    {
        private readonly INavigationService _navigationService;

        private readonly IAppVersionInfoService? _iAppVersionInfoService;

        private readonly NavigationTransitionInfo _defaultTransition = new DrillInNavigationTransitionInfo();

        public EBRestarter(INavigationService navigationService, IAppVersionInfoService iAppVersionInfoService)
        {
            InitializeComponent();

            this.ConfigureTitleBarColors();

            NavigationFrame.CacheSize = 10;

            _navigationService = navigationService;

            _iAppVersionInfoService = iAppVersionInfoService;

            _navigationService.AttachFrame(NavigationFrame);

            _navigationService.NavigateTo("CommonOverview", transitionInfo: new DrillInNavigationTransitionInfo());

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "eB Restarter.ico");

            if (File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            TxbVersion.Text = "v" + _iAppVersionInfoService?.GetAppVersion();


        }

        private void AppTitleBar_BackRequested(TitleBar sender, object args)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(args);

            if (NavigationFrame.CanGoBack)
            {
                NavigationFrame.GoBack();
            }
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(args);

            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            ArgumentNullException.ThrowIfNull(sender);
            ArgumentNullException.ThrowIfNull(args);


            if (args.InvokedItemContainer is NavigationViewItem nvi
                && nvi.Tag is string tag)
            {
                _navigationService.NavigateTo(
                    tag,
                    parameter: null!,
                    transitionInfo: _defaultTransition
                );
            }
        }
    }
}

