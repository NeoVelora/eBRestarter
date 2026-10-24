namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

public sealed record ToggleEdgeStartupBoostResponse(bool Success, bool NewState, string ErrorMessage = "");

