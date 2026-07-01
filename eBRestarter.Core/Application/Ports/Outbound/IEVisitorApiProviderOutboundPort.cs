using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Outbound;

public interface IEVisitorApiProviderOutboundPort
{
    Task<IpInfoData?> RetrieveIpInfoAsync();

    Task<EarningsData?> RetrieveEarningsAsync();
}
