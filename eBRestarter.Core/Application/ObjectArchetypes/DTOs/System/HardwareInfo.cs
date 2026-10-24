namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

public sealed record HardwareInfo
{
    public string ProcessorName { get; init; } = "Unknown";

    public string GraphicsCardName { get; init; } = "Unknown";

    public string InstalledRam { get; init; } = "0 bytes";
}