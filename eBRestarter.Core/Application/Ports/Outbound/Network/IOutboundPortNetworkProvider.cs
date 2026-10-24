using System.Net.NetworkInformation;

namespace eBRestarter.Core.Application.Ports.Outbound.Network;

public interface IOutboundPortNetworkProvider
{
    bool CheckIsNetworkAvailable();
    NetworkInterface[] RetrieveAllNetworkInterfaces();
}


