namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

public sealed record ScheduleBrowserCleanupResponse(bool IsActive, DateTime? NextDate);

