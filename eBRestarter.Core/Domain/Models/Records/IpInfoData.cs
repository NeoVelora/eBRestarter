namespace eBRestarter.Core.Domain.Models.Records;

public record IpInfoData(
    string IpAddress,
    string Hostname,
    string CountryCode,
    string CountryName
);
