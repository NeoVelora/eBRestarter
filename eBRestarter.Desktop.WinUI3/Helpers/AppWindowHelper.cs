using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.Helpers
{
    public static class AppWindowHelper
    {
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
    }
}
