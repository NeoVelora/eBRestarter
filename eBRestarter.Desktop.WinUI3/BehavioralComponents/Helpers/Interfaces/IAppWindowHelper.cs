using Microsoft.UI.Xaml;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Helpers.Interfaces;

public interface IAppWindowHelper
{
    void ConfigureTitleBarColors(Window window);

    void UpdateTitleBarTheme(Window window, ElementTheme theme);
}

