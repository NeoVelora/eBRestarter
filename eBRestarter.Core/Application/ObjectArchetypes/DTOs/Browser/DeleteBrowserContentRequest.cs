using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

public sealed record DeleteBrowserContentRequest(
    BrowserType BrowserType,
    bool DeleteCookies,
    bool DeleteCache,
    bool ForceCloseProcess);

