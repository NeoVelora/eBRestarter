namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.AutoLogon;

public sealed record ConfigureAutoLogonRequest(
    bool IsDeactivateAction,
    bool DisablePasswordlessMode = false,
    bool RestorePasswordlessMode = false,
    string? Username = null,
    string? Domain = null,
    string? Password = null
)
{
#pragma warning disable S2068 // Hard-coded credentials are security-sensitive
    public override string ToString()
        => $"{nameof(ConfigureAutoLogonRequest)} {{ {nameof(IsDeactivateAction)} = {IsDeactivateAction}, "
           + $"{nameof(DisablePasswordlessMode)} = {DisablePasswordlessMode}, "
           + $"{nameof(RestorePasswordlessMode)} = {RestorePasswordlessMode}, "
           + $"{nameof(Username)} = {Username}, {nameof(Domain)} = {Domain}, {nameof(Password)} = ***** }}";
#pragma warning restore S2068
}
