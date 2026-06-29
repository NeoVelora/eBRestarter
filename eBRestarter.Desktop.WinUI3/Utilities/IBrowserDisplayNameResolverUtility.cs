using eBRestarter.Desktop.WinUI3.Utilities;
using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Desktop.WinUI3.Utilities;

public interface IBrowserDisplayNameResolverUtility
{
    BrowserType ResolveBrowserTypeFromDisplayName(string displayName, string defaultDisplayText);
}

