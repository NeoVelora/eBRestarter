namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

public readonly record struct DeleteBrowserContentProgress(string StatusMessage, int CompletedSteps, int TotalSteps);
