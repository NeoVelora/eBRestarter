namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

public sealed record DownloadProgressStatus(long BytesReceived, long TotalBytes, double Percentage);
