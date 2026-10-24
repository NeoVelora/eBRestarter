namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

public sealed record BrowserPaths(
    List<string> CacheDirs,
    List<string> CookiesDirs,
    List<string> ExtensionsDirs
);
