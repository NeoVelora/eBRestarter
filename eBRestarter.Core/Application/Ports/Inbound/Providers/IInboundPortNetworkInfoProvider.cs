using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IInboundPortNetworkInfoProvider
{
    bool IsNetworkAvailable();

    IEnumerable<NetworkStats> RetrieveActiveInterfaces();
}
