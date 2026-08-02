using System;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

using Microsoft.UI.Xaml;

using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handler.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralUIComponents.Helpers.Interfaces;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Handler;

public sealed class ThemeHandler(
    IAppWindowHelper appWindowHelper,
    IMainWindowProvider mainWindowProvider) : IThemeHandler
{
    private const string ThemeDark = "Dark";
    private const string ThemeLight = "Light";

    private readonly IAppWindowHelper _appWindowHelper = appWindowHelper ?? throw new ArgumentNullException(nameof(appWindowHelper));
    private readonly IMainWindowProvider _mainWindowProvider = mainWindowProvider ?? throw new ArgumentNullException(nameof(mainWindowProvider));


    public string CurrentTheme { get; private set; } = ThemeLight;


    public void SetTheme(string themeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeName);

        bool isDark = string.Equals(themeName, ThemeDark, StringComparison.OrdinalIgnoreCase);
        var theme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        var mainWindow = _mainWindowProvider.MainWindow;

        if (mainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
            _appWindowHelper.UpdateTitleBarTheme(mainWindow, theme);
        }

        if (isDark)
        {
            LiveCharts.Configure(config => config.AddDarkTheme());
        }
        else
        {
            LiveCharts.Configure(config => config.AddLightTheme());
        }

        CurrentTheme = themeName;
    }
}
