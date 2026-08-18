namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

public sealed record ApiCredentials(string Username, string ApiKey)
{
    public override string ToString() => $"{nameof(ApiCredentials)} {{ {nameof(Username)} = {Username}, {nameof(ApiKey)} = ***** }}";
}
