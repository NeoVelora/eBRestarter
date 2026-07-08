using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Desktop.WinUI3.Utilities.Interfaces;

public interface IBrowserDisplayNameResolverUtility
{
    BrowserType ResolveBrowserTypeFromDisplayName(string displayName, string defaultDisplayText);
}

