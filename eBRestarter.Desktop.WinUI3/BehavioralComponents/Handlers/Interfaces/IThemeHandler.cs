using eBRestarter.Desktop.WinUI3.ObjectArchetypes.Enums;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Handlers.Interfaces;

public interface IThemeHandler
{
    AppTheme CurrentTheme { get; }

    void SetTheme(AppTheme theme);

    void SetTheme(string themeName);
}
