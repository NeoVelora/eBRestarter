namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

#pragma warning disable S2094

public sealed record DeleteBrowserContentSuccess;

public sealed record DeleteBrowserContentProcessConflict(string Message);

public sealed record DeleteBrowserContentFailure(string Message);

public union DeleteBrowserContentResult(
    DeleteBrowserContentSuccess,
    DeleteBrowserContentProcessConflict,
    DeleteBrowserContentFailure);
