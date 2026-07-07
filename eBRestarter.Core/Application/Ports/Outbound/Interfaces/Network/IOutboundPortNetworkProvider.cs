using System.Net.NetworkInformation;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;

public interface IOutboundPortNetworkProvider
{
    bool CheckIsNetworkAvailable();
    NetworkInterface[] RetrieveAllNetworkInterfaces();
}


