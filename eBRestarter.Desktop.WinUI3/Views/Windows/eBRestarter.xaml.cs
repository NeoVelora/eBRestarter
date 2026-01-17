using eBRestarter.Desktop.WinUI3.Helpers;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ViewModels;
using eBRestarter.Desktop.WinUI3.Views.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.UI;


namespace eBRestarter.Desktop.WinUI3
{
    public sealed partial class EBRestarter : Window
    {
        private readonly INavigationService _navigationService;

        private readonly NavigationTransitionInfo _defaultTransition = new DrillInNavigationTransitionInfo();

        public EBRestarter(INavigationService navigationService, MainViewModel mainViewModel)
        {
            InitializeComponent();

            this.ConfigureTitleBarColors();

            NavigationFrame.CacheSize = 10;

            _navigationService = navigationService;

            _navigationService.AttachFrame(NavigationFrame);

            (this.Content as FrameworkElement)!.DataContext = mainViewModel;

            _navigationService.NavigateTo("CommonOverview", transitionInfo: new DrillInNavigationTransitionInfo());

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }


        }

        private void AppTitleBar_BackRequested(TitleBar sender, object args)
        {
            if (NavigationFrame.CanGoBack)
            {
                NavigationFrame.GoBack();
            }
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                _navigationService.NavigateTo("Settings", transitionInfo: _defaultTransition);
                return;
            }

            if (args.InvokedItemContainer is NavigationViewItem nvi
                && nvi.Tag is string tag)
            {
                _navigationService.NavigateTo(
                    tag,
                    parameter: null,
                    transitionInfo: _defaultTransition
                );
            }
        }
    }
}

