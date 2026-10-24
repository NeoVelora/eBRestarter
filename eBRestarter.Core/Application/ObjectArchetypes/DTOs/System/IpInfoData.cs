namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

public sealed record IpInfoData(
    string IpAddress,
    string Hostname,
    string CountryCode,
    string CountryName
);
