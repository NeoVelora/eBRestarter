using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;

using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Helpers.Interfaces;

namespace eBRestarter.Desktop.WinUI3;

public sealed partial class EBRestarter : Window
{
    private const string ApplicationIconFileName = "eB Restarter.ico";
    private const string AssetsFolderName = "Assets";
    private const string CommonOverviewPageTag = "CommonOverview";
    private const int NavigationFrameCacheSize = 10;

    private readonly IAppWindowHelper _appWindowHelper;
    private readonly IOutboundPortAppVersionInfoProvider? _appVersionInfoService;
    private readonly IMainWindowProvider _mainWindowProvider;
    private readonly INavigationService _navigationService;

    private readonly NavigationTransitionInfo _defaultTransition = new DrillInNavigationTransitionInfo();

    public EBRestarter(
        IAppWindowHelper appWindowHelper,
        IOutboundPortAppVersionInfoProvider appVersionInfoService,
        IMainWindowProvider mainWindowProvider,
        INavigationService navigationService)
    {
        ArgumentNullException.ThrowIfNull(appWindowHelper);
        ArgumentNullException.ThrowIfNull(appVersionInfoService);
        ArgumentNullException.ThrowIfNull(mainWindowProvider);
        ArgumentNullException.ThrowIfNull(navigationService);

        InitializeComponent();

        _appWindowHelper = appWindowHelper;
        _appVersionInfoService = appVersionInfoService;
        _mainWindowProvider = mainWindowProvider;
        _navigationService = navigationService;

        _mainWindowProvider.SetMainWindow(this);

        _appWindowHelper.ConfigureTitleBarColors(this);

        NavigationFrame.CacheSize = NavigationFrameCacheSize;

        _navigationService.AttachFrame(NavigationFrame);

        _navigationService.NavigateTo(CommonOverviewPageTag, transitionInfo: new DrillInNavigationTransitionInfo());

        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

        string iconPath = Path.Combine(AppContext.BaseDirectory, AssetsFolderName, ApplicationIconFileName);

        if (File.Exists(iconPath))
        {
            appWindow.SetIcon(iconPath);
        }

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        TxbVersion.Text = $"v{_appVersionInfoService?.RetrieveAppVersion()}";
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object eventArgs)
    {
        if (!NavigationFrame.CanGoBack)
        {
            return;
        }

        NavigationFrame.GoBack();
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object eventArgs)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(eventArgs);

        if (eventArgs.InvokedItemContainer is not NavigationViewItem navigationViewItem ||
            navigationViewItem.Tag is not string pageTag)
        {
            return;
        }

        _navigationService.NavigateTo(
            pageTag,
            parameter: null!,
            transitionInfo: _defaultTransition);
    }
}
