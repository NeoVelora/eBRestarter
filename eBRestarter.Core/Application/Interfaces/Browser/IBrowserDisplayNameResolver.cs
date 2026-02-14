using eBRestarter.Core.Domain.Enums;

namespace eBRestarter.Core.Application.Interfaces.Browser
{
    public interface IBrowserDisplayNameResolver
    {
        #region PublicMethods

        BrowserType GetBrowserTypeFromDisplayName(string displayName, string defaultDisplayText);

        #endregion
    }
}
