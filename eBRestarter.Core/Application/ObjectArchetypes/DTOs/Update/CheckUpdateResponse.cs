namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Update;

public sealed record CheckUpdateResponse(bool IsUpdateAvailable, string LatestVersion);

