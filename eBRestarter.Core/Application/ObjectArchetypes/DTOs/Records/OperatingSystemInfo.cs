namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

public sealed record OperatingSystemInfo
{
    public string Edition { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string BuildNumber { get; init; } = string.Empty;
}