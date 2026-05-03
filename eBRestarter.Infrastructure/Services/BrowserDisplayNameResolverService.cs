using System;

using eBRestarter.Core.Application.Interfaces.Browser;

using eBRestarter.Core.Application.Enums;


namespace eBRestarter.Infrastructure.Services;


public class BrowserDisplayNameResolverService : IBrowserDisplayNameResolver

{

    public BrowserType GetBrowserTypeFromDisplayName(string displayName, string defaultDisplayText)

    {

        if (string.IsNullOrWhiteSpace(displayName) || displayName == defaultDisplayText || displayName == "Nicht gewählt")

            return BrowserType.Chrome;


        if (Enum.TryParse(displayName, true, out BrowserType type))

            return type;


        return BrowserType.Chrome;

    }

}

