using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound;

public interface IOutboundPortEVisitorApiProvider
{
    Task<IpInfoData?> RetrieveIpInfoAsync();

    Task<EarningsData?> RetrieveEarningsAsync();
}
