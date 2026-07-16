using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Utilities.Interfaces;

public interface IBrowserDisplayNameResolverUtility
{
    BrowserType ResolveBrowserTypeFromDisplayName(string displayName, string defaultDisplayText);
}

