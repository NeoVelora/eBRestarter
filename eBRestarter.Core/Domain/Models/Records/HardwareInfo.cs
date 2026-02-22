namespace eBRestarter.Core.Domain.Models.Records;

public record HardwareInfo
{
    public string ProcessorName { get; init; } = "Unbekannt";
    public string GraphicsCardName { get; init; } = "Unbekannt";
    public string InstalledRam { get; init; } = "0 bytes";
}
