namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

public readonly record struct DeleteBrowserContentProgress(string StatusMessage, int CompletedSteps, int TotalSteps);
