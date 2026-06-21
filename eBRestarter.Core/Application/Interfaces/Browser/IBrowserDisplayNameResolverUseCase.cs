using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Core.Application.Interfaces.Browser;

public interface IBrowserDisplayNameResolverUseCase
{
    BrowserType ResolveBrowserTypeFromDisplayName(string displayName, string defaultDisplayText);
}
