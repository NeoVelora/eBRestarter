using eBRestarter.Core.Application.Constants;

namespace eBRestarter.Core.Domain.Entities;

public sealed record ExtensionConfigDto
{
    public string LANGUAGE { get; init; } = "DE";
    public string ZIEL_URL { get; init; } = WebLinks.EVisitorSurflink;
    public int WARTEZEIT_MS { get; init; } = 180000;
}
