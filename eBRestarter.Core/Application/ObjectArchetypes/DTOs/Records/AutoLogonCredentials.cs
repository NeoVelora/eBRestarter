namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

public sealed record AutoLogonCredentials(string Username, string Domain, string Password)
{
#pragma warning disable S2068 // Hard-coded credentials are security-sensitive
    public override string ToString()
        => $"{nameof(AutoLogonCredentials)} {{ {nameof(Username)} = {Username}, {nameof(Domain)} = {Domain}, {nameof(Password)} = ***** }}";
#pragma warning restore S2068
}
