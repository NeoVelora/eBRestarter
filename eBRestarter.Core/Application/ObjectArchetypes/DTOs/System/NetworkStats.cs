namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

public sealed record NetworkStats(string Name, long BytesReceived, long BytesSent, bool IsActive);
