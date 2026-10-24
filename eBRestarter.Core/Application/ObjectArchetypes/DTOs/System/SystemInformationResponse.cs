namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

public record SystemInformationResponse(
    string ProcessorName,
    string GraphicsCardName,
    string InstalledRam,
    string OsEdition,
    string OsDisplayVersion,
    string OsBuildVersion,
    string StandardBrowserName
);


