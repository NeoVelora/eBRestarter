using System.Net.NetworkInformation;

namespace eBRestarter.Core.Application.Ports.Outbound.Network;

public interface INetworkProviderOutboundPort
{
    bool CheckIsNetworkAvailable();
    NetworkInterface[] RetrieveAllNetworkInterfaces();
}


