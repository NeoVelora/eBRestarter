using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces;

public interface IOutboundPortEVisitorApiProvider
{
    Task<IpInfoData?> RetrieveIpInfoAsync();

    Task<EarningsData?> RetrieveEarningsAsync();
}
