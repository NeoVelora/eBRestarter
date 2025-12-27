using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Domain.Models.Records
{
    public record NetworkStats(string Name, long BytesReceived, long BytesSent, bool IsActive);
}
