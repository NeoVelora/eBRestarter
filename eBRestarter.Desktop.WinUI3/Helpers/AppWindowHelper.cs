using Microsoft.UI.Xaml;

namespace eBRestarter.Desktop.WinUI3.Helpers;

public static class AppWindowHelper
{
    #region PublicMethods

    public static void ConfigureTitleBarColors(this Window window)
    {
        var appWindow = window.AppWindow;
        var titleBar = appWindow.TitleBar;

        var bg = Microsoft.UI.ColorHelper.FromArgb(255, 32, 37, 54);

        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.BackgroundColor = bg;

        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(null);
    }

    #endregion
}
