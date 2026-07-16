using eBRestarter.Desktop.WinUI3.BehavioralUIComponents.Helpers.Interfaces;
using Microsoft.UI.Xaml;

namespace eBRestarter.Desktop.WinUI3.BehavioralUIComponents.Helpers;

public sealed class AppWindowHelper : IAppWindowHelper
{
    public void ConfigureTitleBarColors(Window window)
    {
        var appWindow = window.AppWindow;
        var titleBar = appWindow.TitleBar;

        var bg = Microsoft.UI.ColorHelper.FromArgb(255, 32, 37, 54);

        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.BackgroundColor = bg;

        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(null);
    }

    public void UpdateTitleBarTheme(Window window, ElementTheme theme)
    {
        var titleBar = window.AppWindow.TitleBar;

        if (theme == ElementTheme.Dark)
        {
            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(51, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(77, 255, 255, 255);
            titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
        }
        else
        {
            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(51, 0, 0, 0);
            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(77, 0, 0, 0);
            titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
        }
    }
}
