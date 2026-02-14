using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Domain.Enums;

namespace eBRestarter.Core.Application.Services
{
    public class BrowserDisplayNameResolverService : IBrowserDisplayNameResolver
    {
        #region PublicAndProtectedMethods

        public BrowserType GetBrowserTypeFromDisplayName(string displayName, string defaultDisplayText)
        {
            if (string.IsNullOrWhiteSpace(displayName) || displayName == defaultDisplayText || displayName == "Nicht gewählt")
                return BrowserType.Chrome;

            if (Enum.TryParse(displayName, true, out BrowserType type))
                return type;

            return BrowserType.Chrome;
        }

        #endregion
    }
}
