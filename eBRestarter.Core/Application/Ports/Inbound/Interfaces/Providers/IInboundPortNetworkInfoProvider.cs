using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;

public interface IInboundPortNetworkInfoProvider
{
    bool IsNetworkAvailable();
    IEnumerable<NetworkStats> RetrieveActiveInterfaces();
}
