namespace eBRestarter.Core.Application.Models;

public sealed class RestartTaskDisplayState
{
    #region PublicProperties

    public string Username { get; init; } = "-";
    public string ChoosenBrowser { get; init; } = string.Empty;
    public int RuntimeSeconds { get; init; }
    public int PauseSeconds { get; init; }
    public bool DeleteBrowserContentIsActive { get; init; }
    public string DeleteIsActivatedMessage { get; init; } = string.Empty;
    public string NextDeletionProcessMessage { get; init; } = string.Empty;
    public string NextDeletionProcessDateMessage { get; init; } = string.Empty;
    public bool CheckBrowserAliveRoutine { get; init; }

    #endregion
}
