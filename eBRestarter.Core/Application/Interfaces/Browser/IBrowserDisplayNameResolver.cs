using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Core.Application.Interfaces.Browser;

public interface IBrowserDisplayNameResolver
{


    BrowserType GetBrowserTypeFromDisplayName(string displayName, string defaultDisplayText);

}
