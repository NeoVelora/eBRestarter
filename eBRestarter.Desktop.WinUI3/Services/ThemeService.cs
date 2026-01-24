using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Services
{
    public class ThemeService : IThemeService
    {

        public string CurrentTheme { get; private set; } = "Light";

        public void SetTheme(string themeName)
        {
            if (App.MainWindoweBRestarter?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = themeName == "Dark" ? ElementTheme.Dark : ElementTheme.Light;

            }

            CurrentTheme = themeName;
        }
    }
}
