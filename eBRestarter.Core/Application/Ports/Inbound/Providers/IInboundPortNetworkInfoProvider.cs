using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IInboundPortNetworkInfoProvider
{
    bool IsNetworkAvailable();
    IEnumerable<NetworkStats> RetrieveActiveInterfaces();
}
