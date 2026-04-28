namespace eBRestarter.Core.Application.Models.Records;

public record NetworkStats(string Name, long BytesReceived, long BytesSent, bool IsActive);
