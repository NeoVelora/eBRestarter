using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

namespace eBRestarter.Core.Application.Ports.Outbound.Network;

public interface IOutboundPortEVisitorApiProvider
{
    Task<IpInfoData?> RetrieveIpInfoAsync();

    Task<EarningsData?> RetrieveEarningsAsync();
}
