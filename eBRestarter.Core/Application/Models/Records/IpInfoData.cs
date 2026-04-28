namespace eBRestarter.Core.Application.Models.Records;

public record IpInfoData(
    string IpAddress,
    string Hostname,
    string CountryCode,
    string CountryName
);
