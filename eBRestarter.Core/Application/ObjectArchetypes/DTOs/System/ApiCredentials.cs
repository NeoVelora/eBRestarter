namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

public sealed record ApiCredentials(string Username, string ApiKey)
{
    public override string ToString() => $"{nameof(ApiCredentials)} {{ {nameof(Username)} = {Username}, {nameof(ApiKey)} = ***** }}";
}
